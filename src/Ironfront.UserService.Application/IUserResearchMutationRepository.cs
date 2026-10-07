using Ironfront.UserService.Domain.Entities;

namespace Ironfront.UserService.Application;

public interface IUserResearchMutationRepository
{
    Task<UserTechTreeState?> FindTechTreeStateForUpdateAsync(
        Guid userId,
        string techTreeId,
        CancellationToken cancellationToken);

    void AddTechTreeState(
        UserTechTreeState techTreeState);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}