using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;


namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleServerCommandInbox
    : IBattleServerCommandInbox
{
    private readonly IBattleServerSlotRepository slotRepository;
    private readonly IBattleServerCommandRepository commandRepository;
    private readonly TimeProvider timeProvider;

    public BattleServerCommandInbox(
        IBattleServerSlotRepository slotRepository,
        IBattleServerCommandRepository commandRepository,
        TimeProvider timeProvider)
    {
        this.slotRepository = slotRepository
            ?? throw new ArgumentNullException(
                nameof(slotRepository));

        this.commandRepository = commandRepository
            ?? throw new ArgumentNullException(
                nameof(commandRepository));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public async Task<BattleServerCommandSnapshot?> PollNextAsync(
        string battleServerInstanceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            battleServerInstanceId);

        string normalizedServerInstanceId =
            battleServerInstanceId.Trim();

        BattleServerSlot? slot =
            await slotRepository.FindByServerInstanceIdAsync(
                normalizedServerInstanceId,
                cancellationToken);

        if (slot is null)
        {
            throw new BattleServerSlotNotFoundException(
                normalizedServerInstanceId);
        }

        /*
         * This deliberately returns Pending OR Delivered commands.
         *
         * Delivery is at-least-once. If the BattleServer performs an
         * action but its acknowledgement gets lost, the same CommandId
         * will appear again until an acknowledgement reaches us.
         *
         * The BattleServer will therefore treat CommandId as an
         * idempotency key.
         */
        BattleServerCommand? command =
            await commandRepository
                .GetNextDeliverableCommandAsync(
                    normalizedServerInstanceId,
                    cancellationToken);

        if (command is null)
            return null;

        command.MarkDelivered(
            timeProvider.GetUtcNow().UtcDateTime);

        await commandRepository.SaveChangesAsync(
            cancellationToken);

        return ToSnapshot(command);
    }

    public async Task<BattleServerCommandSnapshot>
        AcknowledgeAsync(
            string battleServerInstanceId,
            BattleServerCommandAcknowledgement acknowledgement,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            battleServerInstanceId);

        ArgumentNullException.ThrowIfNull(
            acknowledgement);

        string normalizedServerInstanceId =
            battleServerInstanceId.Trim();

        BattleServerCommand? command =
            await commandRepository.FindByIdAsync(
                acknowledgement.CommandId,
                cancellationToken);

        if (command is null)
        {
            throw new BattleServerCommandNotFoundException(
                acknowledgement.CommandId);
        }

        if (!string.Equals(
                command.BattleServerInstanceId,
                normalizedServerInstanceId,
                StringComparison.Ordinal))
        {
            throw new BattleServerCommandOwnershipException(
                acknowledgement.CommandId,
                normalizedServerInstanceId);
        }

        DateTime utcNow =
            timeProvider.GetUtcNow().UtcDateTime;

        if (acknowledgement.Succeeded)
        {
            command.Acknowledge(utcNow);
        }
        else
        {
            command.Fail(
                acknowledgement.FailureReason ??
                "Battle server reported an unspecified command failure.",
                utcNow);
        }

        await commandRepository.SaveChangesAsync(
            cancellationToken);

        return ToSnapshot(command);
    }

    private static BattleServerCommandSnapshot ToSnapshot(
        BattleServerCommand command)
    {
        return new BattleServerCommandSnapshot(
            command.CommandId,
            command.BattleServerInstanceId,
            command.CommandType,
            command.PayloadJson,
            command.State,
            command.CreatedAtUtc,
            command.DeliveredAtUtc,
            command.AcknowledgedAtUtc,
            command.FailedAtUtc,
            command.FailureReason);
    }
}