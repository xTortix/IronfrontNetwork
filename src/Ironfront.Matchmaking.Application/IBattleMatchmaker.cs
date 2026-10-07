using Ironfront.Matchmaking.Application.Models;

namespace Ironfront.Matchmaking.Application;

public interface IBattleMatchmaker
{
    Task<BattleMatchSnapshot?> TryFormNextMatchAsync(
        string modeId,
        CancellationToken cancellationToken);
}