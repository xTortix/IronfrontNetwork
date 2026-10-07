using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleMatchLifecycle
    : IBattleMatchLifecycle
{
    private readonly IBattleServerSlotRepository slotRepository;
    private readonly IBattleMatchRepository matchRepository;
    private readonly IMatchmakingUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public BattleMatchLifecycle(
        IBattleServerSlotRepository slotRepository,
        IBattleMatchRepository matchRepository,
        IMatchmakingUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.slotRepository = slotRepository
            ?? throw new ArgumentNullException(
                nameof(slotRepository));

        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public Task<BattleMatchSnapshot> MarkReadyForPlayersAsync(
        string battleServerInstanceId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            battleServerInstanceId);

        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        string normalizedServerInstanceId =
            battleServerInstanceId.Trim();

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                BattleMatch? battleMatch =
                    await matchRepository.FindByIdAsync(
                        matchId,
                        transactionCancellationToken);

                if (battleMatch is null)
                {
                    throw new BattleMatchNotFoundException(
                        matchId);
                }

                if (!string.Equals(
                        battleMatch.BattleServerInstanceId,
                        normalizedServerInstanceId,
                        StringComparison.Ordinal))
                {
                    throw new BattleMatchOwnershipException(
                        matchId,
                        normalizedServerInstanceId);
                }

                BattleServerSlot? slot =
                    await slotRepository
                        .FindByServerInstanceIdAsync(
                            normalizedServerInstanceId,
                            transactionCancellationToken);

                if (slot is null)
                {
                    throw new BattleServerSlotNotFoundException(
                        normalizedServerInstanceId);
                }

                DateTime utcNow =
                    timeProvider.GetUtcNow().UtcDateTime;

                /*
                 * Beide Domain-Methoden sind absichtlich idempotent.
                 * Ein wiederholter Ready-Report für dieselbe MatchId
                 * bleibt dadurch sicher.
                 */
                battleMatch.MarkWaitingForPlayers(
                    utcNow);

                slot.MarkMatchWaitingForPlayers(
                    matchId,
                    utcNow);

                return ToSnapshot(battleMatch);
            },
            cancellationToken);
    }

    public Task<BattleMatchSnapshot> CancelAsync(
    string battleServerInstanceId,
    Guid matchId,
    string? reason,
    CancellationToken cancellationToken)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(
        battleServerInstanceId);

    if (matchId == Guid.Empty)
    {
        throw new ArgumentException(
            "Match id must not be empty.",
            nameof(matchId));
    }

    string normalizedServerInstanceId =
        battleServerInstanceId.Trim();

    return unitOfWork.ExecuteInTransactionAsync(
        async transactionCancellationToken =>
        {
            BattleMatch? battleMatch =
                await matchRepository.FindByIdAsync(
                    matchId,
                    transactionCancellationToken);

            if (battleMatch is null)
            {
                throw new BattleMatchNotFoundException(
                    matchId);
            }

            if (!string.Equals(
                    battleMatch.BattleServerInstanceId,
                    normalizedServerInstanceId,
                    StringComparison.Ordinal))
            {
                throw new BattleMatchOwnershipException(
                    matchId,
                    normalizedServerInstanceId);
            }
            
            if (battleMatch.Status == BattleMatchStatus.Cancelled)
            {
                return ToSnapshot(battleMatch);
            }

            BattleServerSlot? slot =
                await slotRepository
                    .FindByServerInstanceIdAsync(
                        normalizedServerInstanceId,
                        transactionCancellationToken);

            if (slot is null)
            {
                throw new BattleServerSlotNotFoundException(
                    normalizedServerInstanceId);
            }

            DateTime utcNow =
                timeProvider.GetUtcNow().UtcDateTime;

            battleMatch.MarkCancelled(
                reason,
                utcNow);

            slot.ReleaseReservedMatch(
                matchId,
                utcNow);

            return ToSnapshot(battleMatch);
        },
        cancellationToken);
}
    
    private static BattleMatchSnapshot ToSnapshot(
        BattleMatch battleMatch)
    {
        return new BattleMatchSnapshot(
            battleMatch.MatchId,
            battleMatch.BattleServerInstanceId,
            battleMatch.ModeId,
            battleMatch.ModeRevision,
            battleMatch.MapId,
            battleMatch.ExpectedPlayerCount,
            ToApiStatus(battleMatch.Status),
            battleMatch.CreatedAtUtc,
            battleMatch.UpdatedAtUtc,
            battleMatch.WaitingForPlayersAtUtc,
            battleMatch.StartedAtUtc,
            battleMatch.FinishedAtUtc,
            battleMatch.FailureReason);
    }

    private static string ToApiStatus(
        BattleMatchStatus status)
    {
        return status switch
        {
            BattleMatchStatus.Provisioning => "provisioning",

            BattleMatchStatus.WaitingForPlayers =>
                "waiting_for_players",

            BattleMatchStatus.Running => "running",
            BattleMatchStatus.Finishing => "finishing",
            BattleMatchStatus.Completed => "completed",
            BattleMatchStatus.Failed => "failed",
            BattleMatchStatus.Cancelled => "cancelled",

            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown battle match status.")
        };
    }
}