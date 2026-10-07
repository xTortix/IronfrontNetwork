using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application;

public interface IBattleMatchRepository
{
    Task<BattleMatch?> FindByIdAsync(
        Guid matchId,
        CancellationToken cancellationToken);

    Task<BattleMatch?> FindByIdForUpdateAsync(
        Guid matchId,
        CancellationToken cancellationToken);

    void Add(BattleMatch battleMatch);
}