using Ironfront.UserService.Application;
using Ironfront.UserService.Domain.Entities;
using Ironfront.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.UserService.Infrastructure.Repositories;

public sealed class EfUserResearchRepository : IUserResearchRepository
{
    private readonly UserDbContext dbContext;

    public EfUserResearchRepository(UserDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<bool> ProfileExistsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles.AnyAsync(
            profile => profile.UserId == userId,
            cancellationToken);
    }

    public Task<UserTechTreeState?> FindTechTreeStateAsync(
        Guid userId,
        string techTreeId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(techTreeId);

        string normalizedTechTreeId = techTreeId.Trim();

        return dbContext.UserTechTreeStates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                state => state.UserId == userId
                         && state.TechTreeId == normalizedTechTreeId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserResearchNode>> GetResearchNodesAsync(
        Guid userId,
        IReadOnlyCollection<string> nodeIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        string[] normalizedNodeIds = nodeIds
            .Where(nodeId => !string.IsNullOrWhiteSpace(nodeId))
            .Select(nodeId => nodeId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedNodeIds.Length == 0)
        {
            return Array.Empty<UserResearchNode>();
        }

        return await dbContext.UserResearchNodes
            .AsNoTracking()
            .Where(node =>
                node.UserId == userId
                && normalizedNodeIds.Contains(node.NodeId))
            .OrderBy(node => node.NodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetOwnedVehicleIdsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserVehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.UserId == userId)
            .Select(vehicle => vehicle.VehicleId)
            .OrderBy(vehicleId => vehicleId)
            .ToListAsync(cancellationToken);
    }
}