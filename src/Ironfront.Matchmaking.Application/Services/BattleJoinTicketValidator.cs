using System.Security.Cryptography;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleJoinTicketValidator
    : IBattleJoinTicketValidator
{
    private const int SecretByteLength = 32;
    private const int MaximumSecretBase64Length = 128;

    private readonly IBattleMatchRepository matchRepository;
    private readonly IBattleMatchPlayerRepository matchPlayerRepository;
    private readonly IBattleJoinTicketRepository ticketRepository;
    private readonly IBattleServerSlotRepository slotRepository;
    private readonly IMatchmakingUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public BattleJoinTicketValidator(
        IBattleMatchRepository matchRepository,
        IBattleMatchPlayerRepository matchPlayerRepository,
        IBattleJoinTicketRepository ticketRepository,
        IBattleServerSlotRepository slotRepository,
        IMatchmakingUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

        this.matchPlayerRepository = matchPlayerRepository
            ?? throw new ArgumentNullException(
                nameof(matchPlayerRepository));

        this.ticketRepository = ticketRepository
            ?? throw new ArgumentNullException(
                nameof(ticketRepository));

        this.slotRepository = slotRepository
            ?? throw new ArgumentNullException(
                nameof(slotRepository));

        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public Task<BattleJoinTicketValidationSnapshot>
        ValidateAndConsumeAsync(
            string battleServerInstanceId,
            ValidateBattleJoinTicketRequest request,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            battleServerInstanceId);

        ArgumentNullException.ThrowIfNull(request);

        if (request.MatchId == Guid.Empty ||
            request.TicketId == Guid.Empty)
        {
            return Task.FromResult(
                Reject("invalid_ticket"));
        }

        string normalizedServerInstanceId =
            battleServerInstanceId.Trim();

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                BattleJoinTicket? ticket =
                    await ticketRepository
                        .FindByIdForUpdateAsync(
                            request.TicketId,
                            transactionCancellationToken);

                if (ticket is null)
                    return Reject("invalid_ticket");

                if (ticket.MatchId != request.MatchId ||
                    !string.Equals(
                        ticket.BattleServerInstanceId,
                        normalizedServerInstanceId,
                        StringComparison.Ordinal))
                {
                    return Reject("invalid_ticket");
                }

                if (ticket.Status !=
                    BattleJoinTicketStatus.Issued)
                {
                    return Reject("invalid_ticket");
                }

                if (!TryDecodeSecret(
                        request.SecretBase64,
                        out byte[] secret))
                {
                    return Reject("invalid_ticket");
                }

                byte[] suppliedSecretHash =
                    SHA256.HashData(secret);

                if (!CryptographicOperations.FixedTimeEquals(
                        suppliedSecretHash,
                        ticket.SecretHash))
                {
                    return Reject("invalid_ticket");
                }

                BattleMatch? battleMatch =
                    await matchRepository
                        .FindByIdForUpdateAsync(
                            ticket.MatchId,
                            transactionCancellationToken);

                if (battleMatch is null ||
                    battleMatch.Status !=
                    BattleMatchStatus.WaitingForPlayers)
                {
                    return Reject(
                        "match_not_accepting_players");
                }

                BattleServerSlot? slot =
                    await slotRepository
                        .FindByServerInstanceIdAsync(
                            normalizedServerInstanceId,
                            transactionCancellationToken);

                if (slot is null ||
                    slot.Status !=
                    BattleServerSlotStatus.WaitingForPlayers ||
                    slot.ActiveMatchId != ticket.MatchId)
                {
                    return Reject(
                        "match_not_accepting_players");
                }

                BattleMatchPlayer? matchPlayer =
                    await matchPlayerRepository
                        .FindByIdForUpdateAsync(
                            ticket.MatchPlayerId,
                            transactionCancellationToken);

                if (matchPlayer is null ||
                    matchPlayer.MatchId != ticket.MatchId ||
                    !matchPlayer.CanReceiveJoinTicket)
                {
                    return Reject("invalid_ticket");
                }

                DateTime utcNow =
                    timeProvider.GetUtcNow().UtcDateTime;

                if (!ticket.TryConsume(utcNow))
                {
                    return Reject(
                        ticket.Status ==
                        BattleJoinTicketStatus.Expired
                            ? "ticket_expired"
                            : "invalid_ticket");
                }

                matchPlayer.MarkConnected(utcNow);

                return new BattleJoinTicketValidationSnapshot(
                    true,
                    string.Empty,
                    ticket.MatchId,
                    matchPlayer.MatchPlayerId,
                    matchPlayer.UserId,
                    matchPlayer.InitialVehicleId,
                    matchPlayer.TeamId,
                    matchPlayer.TeamSlotIndex,
                    matchPlayer.PlayerSlotIndex);
            },
            cancellationToken);
    }

    private static bool TryDecodeSecret(
        string? secretBase64,
        out byte[] secret)
    {
        secret = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(secretBase64) ||
            secretBase64.Length >
            MaximumSecretBase64Length)
        {
            return false;
        }

        try
        {
            secret = Convert.FromBase64String(
                secretBase64);

            return secret.Length == SecretByteLength;
        }
        catch (FormatException)
        {
            secret = Array.Empty<byte>();
            return false;
        }
    }

    private static BattleJoinTicketValidationSnapshot Reject(
        string rejectionCode)
    {
        return new BattleJoinTicketValidationSnapshot(
            false,
            rejectionCode,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }
}