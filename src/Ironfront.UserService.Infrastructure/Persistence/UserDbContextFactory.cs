using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ironfront.UserService.Infrastructure.Persistence;

public sealed class UserDbContextFactory
: IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        string? connectionString =
        Environment.GetEnvironmentVariable(
            "IRONFRONT_USER_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing environment variable IRONFRONT_USER_DB_CONNECTION.");
        }

        var optionsBuilder =
        new DbContextOptionsBuilder<UserDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new UserDbContext(optionsBuilder.Options);
    }
}
