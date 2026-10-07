using Ironfront.Matchmaking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ironfront.Matchmaking.Application.DependencyInjection;

public static class MatchmakingApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddMatchmakingApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<
            IBattleServerRegistry,
            BattleServerRegistry>();

        services.AddScoped<
            IBattleServerCommandInbox,
            BattleServerCommandInbox>();
        
        services.AddScoped<
            IBattleMatchProvisioner,
            BattleMatchProvisioner>();
        
        services.AddScoped<
            IBattleMatchLifecycle,
            BattleMatchLifecycle>();
        
        services.AddScoped<
            IBattleServerHealthReconciler,
            BattleServerHealthReconciler>();
        
        services.AddScoped<
            IBattleJoinTicketIssuer,
            BattleJoinTicketIssuer>();
        
        services.AddScoped<
            IBattleJoinTicketValidator,
            BattleJoinTicketValidator>();
        
        services.AddScoped<
            IBattleMatchPlayerConnectionLifecycle,
            BattleMatchPlayerConnectionLifecycle>();
        
        services.AddScoped<
            IBattleMatchmaker,
            BattleMatchmaker>();
        
        services.AddScoped<
            IBattleQueueLifecycle,
            BattleQueueLifecycle>();
        
        return services;
    }
}