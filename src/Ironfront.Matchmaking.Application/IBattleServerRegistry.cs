using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleServerRegistry
{
    Task<BattleServerSlotSnapshot> RegisterAsync(
        BattleServerRegistrationCommand command,
        CancellationToken cancellationToken);

    Task<BattleServerSlotSnapshot> RecordHeartbeatAsync(
        BattleServerHeartbeatCommand command,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BattleServerSlotSnapshot>> GetReadySlotsAsync(
        TimeSpan maximumHeartbeatAge,
        CancellationToken cancellationToken);
}