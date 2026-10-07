using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Ironfront.Matchmaking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ironfront.Matchmaking.Infrastructure.DependencyInjection;

public static class
    MatchmakingInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMatchmakingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        services.AddDbContext<MatchmakingDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<
            IBattleServerSlotRepository,
            EfBattleServerSlotRepository>();
        
        services.AddScoped<
            IBattleServerCommandRepository,
            EfBattleServerCommandRepository>();

        services.AddScoped<
            IBattleMatchRepository,
            EfBattleMatchRepository>();

        services.AddScoped<
            IMatchmakingUnitOfWork,
            EfMatchmakingUnitOfWork>();
        
        services.AddScoped<
            IBattleMatchPlayerRepository,
            EfBattleMatchPlayerRepository>();

        services.AddScoped<
            IBattleJoinTicketRepository,
            EfBattleJoinTicketRepository>();
        
        services.AddScoped<
            IBattleQueueEntryRepository,
            EfBattleQueueEntryRepository>();
        
        return services;
    }
}