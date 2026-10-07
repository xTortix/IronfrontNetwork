using Ironfront.UserService.Application.Models;

namespace Ironfront.UserService.Application;

public interface IUserTechTreeProgressService
{
    Task<UserTechTreeProgressSnapshot> GetProgressAsync(
        Guid userId,
        string techTreeId,
        CancellationToken cancellationToken);
}