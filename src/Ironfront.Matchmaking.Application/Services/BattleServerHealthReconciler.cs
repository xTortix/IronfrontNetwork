using Ironfront.Matchmaking.Application.Configuration;
using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleServerHealthReconciler
    : IBattleServerHealthReconciler
{
    private const string HeartbeatTimeoutFailureReason =
        "Battle server heartbeat timed out.";

    private readonly IBattleServerSlotRepository slotRepository;
    private readonly IBattleMatchRepository matchRepository;
    private readonly IMatchmakingUnitOfWork unitOfWork;
    private readonly BattleServerSelectionPolicy selectionPolicy;
    private readonly TimeProvider timeProvider;

    public BattleServerHealthReconciler(
        IBattleServerSlotRepository slotRepository,
        IBattleMatchRepository matchRepository,
        IMatchmakingUnitOfWork unitOfWork,
        BattleServerSelectionPolicy selectionPolicy,
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

        this.selectionPolicy = selectionPolicy
            ?? throw new ArgumentNullException(
                nameof(selectionPolicy));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public Task<bool> ReconcileOneStaleSlotAsync(
        CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                DateTime utcNow =
                    timeProvider.GetUtcNow().UtcDateTime;

                DateTime maximumLastHeartbeatUtc =
                    utcNow - selectionPolicy.MaximumHeartbeatAge;

                BattleServerSlot? slot =
                    await slotRepository
                        .TryAcquireStaleForReconciliationAsync(
                            maximumLastHeartbeatUtc,
                            transactionCancellationToken);

                if (slot is null)
                    return false;

                if (slot.ActiveMatchId is not Guid activeMatchId)
                {
                    slot.MarkOffline(utcNow);
                    return true;
                }

                BattleMatch? battleMatch =
                    await matchRepository.FindByIdAsync(
                        activeMatchId,
                        transactionCancellationToken);

                if (battleMatch is null)
                {
                    throw new BattleMatchNotFoundException(
                        activeMatchId);
                }

                /*
                 * An active reservation normally belongs to a
                 * non-terminal match. If old or corrupted data already
                 * contains a terminal match, we still release the stale
                 * slot instead of leaving it stuck forever.
                 */
                if (battleMatch.Status is
                    BattleMatchStatus.Provisioning or
                    BattleMatchStatus.WaitingForPlayers or
                    BattleMatchStatus.Running or
                    BattleMatchStatus.Finishing)
                {
                    battleMatch.MarkFailed(
                        HeartbeatTimeoutFailureReason,
                        utcNow);
                }

                slot.MarkOfflineAndReleaseMatch(
                    activeMatchId,
                    utcNow);

                return true;
            },
            cancellationToken);
    }
}