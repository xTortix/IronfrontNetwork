using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.Matchmaking.Infrastructure.Repositories;

public sealed class EfBattleServerSlotRepository
    : IBattleServerSlotRepository
{
    private readonly MatchmakingDbContext dbContext;

    public EfBattleServerSlotRepository(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
                         ?? throw new ArgumentNullException(
                             nameof(dbContext));
    }

    public Task<BattleServerSlot?> FindByServerInstanceIdAsync(
        string serverInstanceId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleServerSlots
            .SingleOrDefaultAsync(
                slot =>
                    slot.ServerInstanceId ==
                    serverInstanceId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<BattleServerSlot>>
        GetReadySlotsWithHeartbeatSinceAsync(
            DateTime minimumHeartbeatUtc,
            CancellationToken cancellationToken)
    {
        return await dbContext.BattleServerSlots
            .AsNoTracking()
            .Where(slot =>
                slot.Status ==
                BattleServerSlotStatus.Ready &&
                slot.LastHeartbeatUtc >=
                minimumHeartbeatUtc)
            .OrderBy(slot => slot.Region)
            .ThenBy(slot => slot.ServerInstanceId)
            .ToListAsync(cancellationToken);
    }

    public Task<BattleServerSlot?> TryAcquireReadyForReservationAsync(
        DateTime minimumLastHeartbeatUtc,
        int requiredPlayerCapacity,
        CancellationToken cancellationToken)
    {
        if (requiredPlayerCapacity is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredPlayerCapacity),
                "Required player capacity must be between 1 and 128.");
        }

        /*
         * PostgreSQL locks exactly one suitable READY slot.
         *
         * SKIP LOCKED is important once several matchmaking workers exist:
         * one worker locks a slot, all others skip that slot instead of
         * waiting or accidentally reserving it too.
         *
         * The lock remains active until EfMatchmakingUnitOfWork commits or
         * rolls back the surrounding database transaction.
         */
        return dbContext.BattleServerSlots
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM battle_server_slots
                                  WHERE "Status" = {BattleServerSlotStatus.Ready.ToString()}
                                    AND "LastHeartbeatUtc" >= {minimumLastHeartbeatUtc}
                                    AND "MaxPlayers" >= {requiredPlayerCapacity}
                                  ORDER BY "Region", "ServerInstanceId"
                                  FOR UPDATE SKIP LOCKED
                                  LIMIT 1
                                  """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    public Task<BattleServerSlot?> TryAcquireStaleForReconciliationAsync(
        DateTime maximumLastHeartbeatUtc,
        CancellationToken cancellationToken)
    {
        /*
         * One worker locks exactly one stale slot.
         *
         * SKIP LOCKED prevents multiple MatchmakingService instances
         * from reconciling the same BattleServer slot concurrently.
         */
        return dbContext.BattleServerSlots
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM battle_server_slots
                                  WHERE "LastHeartbeatUtc" < {maximumLastHeartbeatUtc}
                                    AND "Status" IN
                                    (
                                        {BattleServerSlotStatus.Ready.ToString()},
                                        {BattleServerSlotStatus.Provisioning.ToString()},
                                        {BattleServerSlotStatus.WaitingForPlayers.ToString()},
                                        {BattleServerSlotStatus.Running.ToString()},
                                        {BattleServerSlotStatus.Finishing.ToString()}
                                    )
                                  ORDER BY "LastHeartbeatUtc", "ServerInstanceId"
                                  FOR UPDATE SKIP LOCKED
                                  LIMIT 1
                                  """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    public void Add(BattleServerSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        dbContext.BattleServerSlots.Add(slot);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(
            cancellationToken);
    }
}