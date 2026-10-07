using Ironfront.Matchmaking.Domain.Entities;

namespace Ironfront.Matchmaking.Application;

public interface IBattleServerCommandRepository
{
    Task<BattleServerCommand?> FindByIdAsync(
        Guid commandId,
        CancellationToken cancellationToken);

    Task<BattleServerCommand?> GetNextDeliverableCommandAsync(
        string battleServerInstanceId,
        CancellationToken cancellationToken);

    void Add(BattleServerCommand command);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}