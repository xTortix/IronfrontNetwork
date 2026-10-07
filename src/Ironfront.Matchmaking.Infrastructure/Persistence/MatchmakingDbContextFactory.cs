using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ironfront.Matchmaking.Infrastructure.Persistence;

public sealed class MatchmakingDbContextFactory
    : IDesignTimeDbContextFactory<MatchmakingDbContext>
{
    public MatchmakingDbContext CreateDbContext(
        string[] args)
    {
        string? connectionString =
            Environment.GetEnvironmentVariable(
                "IRONFRONT_MATCHMAKING_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing environment variable " +
                "IRONFRONT_MATCHMAKING_DB_CONNECTION.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<MatchmakingDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new MatchmakingDbContext(
            optionsBuilder.Options);
    }
}