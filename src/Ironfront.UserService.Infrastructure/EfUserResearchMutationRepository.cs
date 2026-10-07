using Ironfront.UserService.Application;
using Ironfront.UserService.Domain.Entities;
using Ironfront.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.UserService.Infrastructure;

public sealed class EfUserResearchMutationRepository
    : IUserResearchMutationRepository
{
    private readonly UserDbContext dbContext;

    public EfUserResearchMutationRepository(
        UserDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<UserTechTreeState?>
        FindTechTreeStateForUpdateAsync(
            Guid userId,
            string techTreeId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(techTreeId))
        {
            throw new ArgumentException(
                "A tech tree ID is required.",
                nameof(techTreeId));
        }

        string normalizedTechTreeId = techTreeId.Trim();

        return dbContext.UserTechTreeStates
            .SingleOrDefaultAsync(
                state =>
                    state.UserId == userId &&
                    state.TechTreeId == normalizedTechTreeId,
                cancellationToken);
    }

    public void AddTechTreeState(
        UserTechTreeState techTreeState)
    {
        ArgumentNullException.ThrowIfNull(techTreeState);

        dbContext.UserTechTreeStates.Add(techTreeState);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(
            cancellationToken);
    }
}