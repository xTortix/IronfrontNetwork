using Ironfront.UserService.Application.Models;

namespace Ironfront.UserService.Application;

public interface IUserTechTreeResearchService
{
    Task<UserTechTreeProgressSnapshot>
        SelectResearchTargetAsync(
            Guid userId,
            string techTreeId,
            string nodeId,
            CancellationToken cancellationToken);
}