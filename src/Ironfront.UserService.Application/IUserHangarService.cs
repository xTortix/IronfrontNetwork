using Ironfront.UserService.Application.Models;

namespace Ironfront.UserService.Application;

public interface IUserHangarService
{
    Task<UserHangarSnapshot> GetHangarAsync(
        Guid userId,
        CancellationToken cancellationToken);
}