using Ironfront.UserService.Domain.Entities;

namespace Ironfront.UserService.Application;

public interface IUserResearchRepository
{
    Task<bool> ProfileExistsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<UserTechTreeState?> FindTechTreeStateAsync(
        Guid userId,
        string techTreeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserResearchNode>> GetResearchNodesAsync(
        Guid userId,
        IReadOnlyCollection<string> nodeIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetOwnedVehicleIdsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}