using Ironfront.UserService.Application.Models;

namespace Ironfront.UserService.Application;

public interface IUserProvisioningService
{
    Task<ProvisionedUserProfile> ProvisionAsync(
        ProvisionUserCommand command,
        CancellationToken cancellationToken);
}
