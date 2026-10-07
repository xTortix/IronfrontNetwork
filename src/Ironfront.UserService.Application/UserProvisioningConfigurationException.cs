namespace Ironfront.UserService.Application;

public sealed class UserProvisioningConfigurationException
: InvalidOperationException
{
    public UserProvisioningConfigurationException(string message)
    : base(message)
    {
    }
}
