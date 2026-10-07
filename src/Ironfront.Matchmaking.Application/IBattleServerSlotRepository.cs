using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application;

public interface IBattleServerSlotRepository
{
    Task<BattleServerSlot?> FindByServerInstanceIdAsync(
        string serverInstanceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BattleServerSlot>>
        GetReadySlotsWithHeartbeatSinceAsync(
            DateTime minimumHeartbeatUtc,
            CancellationToken cancellationToken);

    void Add(BattleServerSlot slot);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
    
    Task<BattleServerSlot?> TryAcquireReadyForReservationAsync(
        DateTime minimumLastHeartbeatUtc,
        int requiredPlayerCapacity,
        CancellationToken cancellationToken);
    
    Task<BattleServerSlot?> TryAcquireStaleForReconciliationAsync(
        DateTime maximumLastHeartbeatUtc,
        CancellationToken cancellationToken);
}