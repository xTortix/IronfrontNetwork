using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application;

public interface IBattleMatchPlayerRepository
{
    Task<BattleMatchPlayer?> FindByIdAsync(
        Guid matchPlayerId,
        CancellationToken cancellationToken);

    Task<BattleMatchPlayer?> FindByMatchAndUserAsync(
        Guid matchId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BattleMatchPlayer>> GetByMatchIdAsync(
        Guid matchId,
        CancellationToken cancellationToken);

    Task<BattleMatchPlayer?> FindByIdForUpdateAsync(
        Guid matchPlayerId,
        CancellationToken cancellationToken);
    
    void Add(BattleMatchPlayer matchPlayer);
}