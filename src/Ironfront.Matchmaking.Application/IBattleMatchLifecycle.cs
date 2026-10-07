using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleMatchLifecycle
{
    Task<BattleMatchSnapshot> MarkReadyForPlayersAsync(
        string battleServerInstanceId,
        Guid matchId,
        CancellationToken cancellationToken);

    Task<BattleMatchSnapshot> CancelAsync(
        string battleServerInstanceId,
        Guid matchId,
        string? reason,
        CancellationToken cancellationToken);
}