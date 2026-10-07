using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.Matchmaking.Infrastructure.Repositories;

public sealed class EfBattleServerCommandRepository
    : IBattleServerCommandRepository
{
    private readonly MatchmakingDbContext dbContext;

    public EfBattleServerCommandRepository(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
                         ?? throw new ArgumentNullException(
                             nameof(dbContext));
    }

    public Task<BattleServerCommand?> FindByIdAsync(
        Guid commandId,
        CancellationToken cancellationToken)
    {
        return dbContext.BattleServerCommands
            .SingleOrDefaultAsync(
                command => command.CommandId == commandId,
                cancellationToken);
    }

    public Task<BattleServerCommand?>
        GetNextDeliverableCommandAsync(
            string battleServerInstanceId,
            CancellationToken cancellationToken)
    {
        return dbContext.BattleServerCommands
            .Where(command =>
                command.BattleServerInstanceId ==
                battleServerInstanceId &&
                (command.State ==
                 BattleServerCommandState.Pending ||
                 command.State ==
                 BattleServerCommandState.Delivered))
            .OrderBy(command => command.CreatedAtUtc)
            .ThenBy(command => command.CommandId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(BattleServerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        dbContext.BattleServerCommands.Add(command);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(
            cancellationToken);
    }
}