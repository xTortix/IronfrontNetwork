using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application;

public interface IBattleQueueEntryRepository
{
    Task<BattleQueueEntry?> FindByIdAsync(
        Guid queueEntryId,
        CancellationToken cancellationToken);

    Task<BattleQueueEntry?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /*
     * Sperrt genau den ältesten wartenden Spieler für einen Modus.
     *
     * Der Lock ist nur innerhalb einer laufenden
     * IMatchmakingUnitOfWork-Transaktion gültig.
     */
    Task<BattleQueueEntry?> TryAcquireOldestQueuedForUpdateAsync(
        string modeId,
        int modeRevision,
        CancellationToken cancellationToken);

    /*
     * Sperrt eine kleine Kandidatenmenge, die BR-technisch für den
     * ersten Spieler überhaupt infrage kommt.
     *
     * Die endgültige gegenseitige Fairness-Prüfung erfolgt danach
     * in BattleRatingMatchmakingWindow.AreCompatible(...).
     */
    Task<IReadOnlyList<BattleQueueEntry>>
        TryAcquireQueuedCandidatesForUpdateAsync(
            string modeId,
            int modeRevision,
            Guid excludedQueueEntryId,
            byte preferredBattleRatingTenths,
            byte minimumBattleRatingTenths,
            byte maximumBattleRatingTenths,
            int maximumCandidateCount,
            CancellationToken cancellationToken);

    void Add(
        BattleQueueEntry queueEntry);
}