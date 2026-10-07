using Ironfront.UserService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ironfront.UserService.Application.DependencyInjection;

public static class UserApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUserApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IUserHangarService, UserHangarService>();
        services.AddScoped<IUserDeckService, UserDeckService>();
        services.AddScoped<IUserTechTreeProgressService, UserTechTreeProgressService>();
        services.AddScoped<IUserTechTreeResearchService, UserTechTreeResearchService>();
        
        return services;
    }
}
