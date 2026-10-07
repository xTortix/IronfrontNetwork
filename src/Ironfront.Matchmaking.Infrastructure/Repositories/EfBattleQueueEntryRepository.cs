using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.Matchmaking.Infrastructure.Repositories;

public sealed class EfBattleQueueEntryRepository
    : IBattleQueueEntryRepository
{
    private readonly MatchmakingDbContext dbContext;

    public EfBattleQueueEntryRepository(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public Task<BattleQueueEntry?> FindByIdAsync(
        Guid queueEntryId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleQueueEntries
            .SingleOrDefaultAsync(
                queueEntry =>
                    queueEntry.QueueEntryId ==
                    queueEntryId,
                cancellationToken);
    }

    public Task<BattleQueueEntry?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleQueueEntries
            .SingleOrDefaultAsync(
                queueEntry =>
                    queueEntry.UserId == userId &&
                    (queueEntry.Status ==
                        BattleQueueEntryStatus.Queued ||
                     queueEntry.Status ==
                        BattleQueueEntryStatus.Matched),
                cancellationToken);
    }

    public Task<BattleQueueEntry?>
        TryAcquireOldestQueuedForUpdateAsync(
            string modeId,
            int modeRevision,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            modeId);

        if (modeRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modeRevision));
        }

        return dbContext.BattleQueueEntries
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM battle_queue_entries
                WHERE "ModeId" = {modeId.Trim()}
                  AND "ModeRevision" = {modeRevision}
                  AND "Status" =
                      {BattleQueueEntryStatus.Queued.ToString()}
                ORDER BY "QueuedAtUtc", "QueueEntryId"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<BattleQueueEntry>>
        TryAcquireQueuedCandidatesForUpdateAsync(
            string modeId,
            int modeRevision,
            Guid excludedQueueEntryId,
            byte preferredBattleRatingTenths,
            byte minimumBattleRatingTenths,
            byte maximumBattleRatingTenths,
            int maximumCandidateCount,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            modeId);

        if (modeRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modeRevision));
        }

        if (excludedQueueEntryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Excluded queue entry id must not be empty.",
                nameof(excludedQueueEntryId));
        }

        if (minimumBattleRatingTenths >
            maximumBattleRatingTenths)
        {
            throw new ArgumentException(
                "Minimum battle rating must not exceed maximum battle rating.");
        }

        if (maximumCandidateCount is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCandidateCount),
                "Candidate count must be between 1 and 32.");
        }
        
        List<BattleQueueEntry> candidates =
            await dbContext.BattleQueueEntries
                .FromSqlInterpolated(
                    $"""
                     SELECT *
                     FROM battle_queue_entries
                     WHERE "ModeId" = {modeId.Trim()}
                       AND "ModeRevision" = {modeRevision}
                       AND "Status" =
                           {BattleQueueEntryStatus.Queued.ToString()}
                       AND "QueueEntryId" <> {excludedQueueEntryId}
                       AND "VehicleBattleRatingTenths" >=
                           {minimumBattleRatingTenths}
                       AND "VehicleBattleRatingTenths" <=
                           {maximumBattleRatingTenths}
                     ORDER BY
                         ABS(
                             CAST(
                                 "VehicleBattleRatingTenths"
                                 AS integer) -
                             {preferredBattleRatingTenths}),
                         "QueuedAtUtc",
                         "QueueEntryId"
                     FOR UPDATE SKIP LOCKED
                     LIMIT {maximumCandidateCount}
                     """)
                .ToListAsync(cancellationToken);

        return candidates;
    }

    public void Add(
        BattleQueueEntry queueEntry)
    {
        ArgumentNullException.ThrowIfNull(
            queueEntry);

        dbContext.BattleQueueEntries.Add(
            queueEntry);
    }
}
