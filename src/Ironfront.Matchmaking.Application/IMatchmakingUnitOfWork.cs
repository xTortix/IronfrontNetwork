namespace Ironfront.Matchmaking.Application;

public interface IMatchmakingUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}