using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.Matchmaking.Infrastructure.Repositories;

public sealed class EfBattleMatchPlayerRepository
    : IBattleMatchPlayerRepository
{
    private readonly MatchmakingDbContext dbContext;

    public EfBattleMatchPlayerRepository(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
                         ?? throw new ArgumentNullException(
                             nameof(dbContext));
    }

    public Task<BattleMatchPlayer?> FindByIdAsync(
        Guid matchPlayerId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleMatchPlayers
            .SingleOrDefaultAsync(
                matchPlayer =>
                    matchPlayer.MatchPlayerId ==
                    matchPlayerId,
                cancellationToken);
    }
    
    public Task<BattleMatchPlayer?> FindByIdForUpdateAsync(
        Guid matchPlayerId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleMatchPlayers
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM battle_match_players
                                  WHERE "MatchPlayerId" = {matchPlayerId}
                                  FOR UPDATE
                                  """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    public Task<BattleMatchPlayer?> FindByMatchAndUserAsync(
        Guid matchId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleMatchPlayers
            .SingleOrDefaultAsync(
                matchPlayer =>
                    matchPlayer.MatchId == matchId &&
                    matchPlayer.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<BattleMatchPlayer>>
        GetByMatchIdAsync(
            Guid matchId,
            CancellationToken cancellationToken)
    {
        return await dbContext.BattleMatchPlayers
            .Where(matchPlayer =>
                matchPlayer.MatchId == matchId)
            .OrderBy(matchPlayer =>
                matchPlayer.PlayerSlotIndex)
            .ToListAsync(cancellationToken);
    }
    
    public void Add(
        BattleMatchPlayer matchPlayer)
    {
        ArgumentNullException.ThrowIfNull(matchPlayer);

        dbContext.BattleMatchPlayers.Add(matchPlayer);
    }
}