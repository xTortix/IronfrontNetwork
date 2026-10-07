using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleServerRegistry
    : IBattleServerRegistry
{
    private readonly IBattleServerSlotRepository repository;
    private readonly TimeProvider timeProvider;

    public BattleServerRegistry(
        IBattleServerSlotRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(
                nameof(repository));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public async Task<BattleServerSlotSnapshot> RegisterAsync(
        BattleServerRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        BattleServerSlot? existingSlot =
            await repository.FindByServerInstanceIdAsync(
                command.ServerInstanceId,
                cancellationToken);

        BattleServerSlot slot;

        if (existingSlot is null)
        {
            slot = BattleServerSlot.Create(
                command.ServerInstanceId,
                command.HostId,
                command.Region,
                command.PublicHost,
                command.PublicPort,
                command.BuildVersion,
                command.CatalogVersion,
                command.MaxPlayers,
                utcNow);

            repository.Add(slot);
        }
        else
        {
            existingSlot.RefreshRegistration(
                command.HostId,
                command.Region,
                command.PublicHost,
                command.PublicPort,
                command.BuildVersion,
                command.CatalogVersion,
                command.MaxPlayers,
                utcNow);

            slot = existingSlot;
        }

        await repository.SaveChangesAsync(
            cancellationToken);

        return ToSnapshot(slot);
    }

    public async Task<BattleServerSlotSnapshot>
        RecordHeartbeatAsync(
            BattleServerHeartbeatCommand command,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        BattleServerSlot? slot =
            await repository.FindByServerInstanceIdAsync(
                command.ServerInstanceId,
                cancellationToken);

        if (slot is null)
        {
            throw new BattleServerSlotNotFoundException(
                command.ServerInstanceId);
        }

        slot.RecordHeartbeat(
            command.PlayerCount,
            timeProvider.GetUtcNow().UtcDateTime);

        await repository.SaveChangesAsync(
            cancellationToken);

        return ToSnapshot(slot);
    }

    public async Task<IReadOnlyList<BattleServerSlotSnapshot>>
        GetReadySlotsAsync(
            TimeSpan maximumHeartbeatAge,
            CancellationToken cancellationToken)
    {
        if (maximumHeartbeatAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHeartbeatAge),
                "Maximum heartbeat age must be greater than zero.");
        }

        DateTime minimumHeartbeatUtc =
            timeProvider.GetUtcNow().UtcDateTime -
            maximumHeartbeatAge;

        IReadOnlyList<BattleServerSlot> slots =
            await repository.GetReadySlotsWithHeartbeatSinceAsync(
                minimumHeartbeatUtc,
                cancellationToken);

        return slots
            .Select(ToSnapshot)
            .ToArray();
    }

    private static BattleServerSlotSnapshot ToSnapshot(
        BattleServerSlot slot)
    {
        return new BattleServerSlotSnapshot(
            slot.ServerInstanceId,
            slot.HostId,
            slot.Region,
            slot.PublicHost,
            slot.PublicPort,
            slot.BuildVersion,
            slot.CatalogVersion,
            slot.Status,
            slot.PlayerCount,
            slot.MaxPlayers,
            slot.ActiveMatchId,
            slot.RegisteredAtUtc,
            slot.LastHeartbeatUtc,
            slot.UpdatedAtUtc);
    }
}