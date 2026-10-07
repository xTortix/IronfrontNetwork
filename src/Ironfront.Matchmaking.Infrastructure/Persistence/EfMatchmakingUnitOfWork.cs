using Ironfront.Matchmaking.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ironfront.Matchmaking.Infrastructure.Persistence;

public sealed class EfMatchmakingUnitOfWork
    : IMatchmakingUnitOfWork
{
    private readonly MatchmakingDbContext dbContext;

    public EfMatchmakingUnitOfWork(
        MatchmakingDbContext dbContext)
    {
        this.dbContext = dbContext
                         ?? throw new ArgumentNullException(
                             nameof(dbContext));
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            T result = await operation(
                cancellationToken);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
}