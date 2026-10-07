namespace Ironfront.Matchmaking.Application;

public interface IBattleServerHealthReconciler
{
    Task<bool> ReconcileOneStaleSlotAsync(
        CancellationToken cancellationToken);
}