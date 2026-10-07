using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.Matchmaking.Infrastructure.Repositories;

public sealed class EfBattleMatchRepository
    : IBattleMatchRepository
{
    private readonly MatchmakingDbContext dbContext;

    public EfBattleMatchRepository(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
                         ?? throw new ArgumentNullException(
                             nameof(dbContext));
    }

    public Task<BattleMatch?> FindByIdAsync(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleMatches
            .SingleOrDefaultAsync(
                battleMatch =>
                    battleMatch.MatchId == matchId,
                cancellationToken);
    }
    
    public Task<BattleMatch?> FindByIdForUpdateAsync(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleMatches
            .FromSqlInterpolated($"""
                                  SELECT *
                                  FROM battle_matches
                                  WHERE "MatchId" = {matchId}
                                  FOR UPDATE
                                  """)
            .SingleOrDefaultAsync(cancellationToken);
    }
    
    public void Add(BattleMatch battleMatch)
    {
        ArgumentNullException.ThrowIfNull(battleMatch);

        dbContext.BattleMatches.Add(battleMatch);
    }
}