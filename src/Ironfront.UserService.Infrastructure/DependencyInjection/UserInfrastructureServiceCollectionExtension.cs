using Ironfront.UserService.Application;
using Ironfront.UserService.Infrastructure.Persistence;
using Ironfront.UserService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ironfront.UserService.Infrastructure.DependencyInjection;

public static class UserInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<UserDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUserHangarRepository, EfUserHangarRepository>();
        services.AddScoped<IUserResearchRepository, EfUserResearchRepository>();
        services.AddScoped<IUserResearchMutationRepository, EfUserResearchMutationRepository>();
        
        return services;
    }
}
