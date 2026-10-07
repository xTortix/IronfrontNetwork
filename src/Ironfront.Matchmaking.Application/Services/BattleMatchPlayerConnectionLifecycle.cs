using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleMatchPlayerConnectionLifecycle
    : IBattleMatchPlayerConnectionLifecycle
{
    private readonly IBattleMatchRepository matchRepository;
    private readonly IBattleMatchPlayerRepository matchPlayerRepository;
    private readonly IMatchmakingUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public BattleMatchPlayerConnectionLifecycle(
        IBattleMatchRepository matchRepository,
        IBattleMatchPlayerRepository matchPlayerRepository,
        IMatchmakingUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

        this.matchPlayerRepository = matchPlayerRepository
            ?? throw new ArgumentNullException(
                nameof(matchPlayerRepository));

        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }
    
    public async Task MarkDisconnectedAsync(
        string battleServerInstanceId,
        Guid matchId,
        Guid matchPlayerId,
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

        if (matchPlayerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match player id must not be empty.",
                nameof(matchPlayerId));
        }

        string normalizedServerInstanceId =
            battleServerInstanceId.Trim();

        await unitOfWork.ExecuteInTransactionAsync<int>(
            async transactionCancellationToken =>
            {
                BattleMatch? battleMatch =
                    await matchRepository.FindByIdForUpdateAsync(
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
                    throw new InvalidOperationException(
                        $"Battle server '{normalizedServerInstanceId}' " +
                        $"does not own match '{matchId:D}'.");
                }

                BattleMatchPlayer? matchPlayer =
                    await matchPlayerRepository
                        .FindByIdForUpdateAsync(
                            matchPlayerId,
                            transactionCancellationToken);

                if (matchPlayer is null)
                {
                    throw new BattleMatchPlayerNotFoundException(
                        matchPlayerId);
                }

                if (matchPlayer.MatchId != battleMatch.MatchId)
                {
                    throw new InvalidOperationException(
                        $"Match player '{matchPlayerId:D}' does not belong " +
                        $"to match '{matchId:D}'.");
                }

                /*
                 * Disconnects müssen idempotent sein:
                 * Kommt dieselbe Meldung mehrfach, bleibt der Status
                 * einfach Disconnected.
                 */
                if (matchPlayer.Status ==
                    BattleMatchPlayerStatus.Disconnected)
                {
                    return 0;
                }

                if (matchPlayer.Status !=
                    BattleMatchPlayerStatus.Connected)
                {
                    throw new InvalidOperationException(
                        $"Match player '{matchPlayerId:D}' cannot be " +
                        $"disconnected from status '{matchPlayer.Status}'.");
                }

                matchPlayer.MarkDisconnected(
                    timeProvider.GetUtcNow().UtcDateTime);

                return 0;
            },
            cancellationToken);
    }
}