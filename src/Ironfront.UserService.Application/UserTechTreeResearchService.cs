using Ironfront.UserService.Application.Models;
using Ironfront.UserService.Domain.Entities;

namespace Ironfront.UserService.Application;

public sealed class UserTechTreeResearchService
    : IUserTechTreeResearchService
{
    private readonly IUserTechTreeProgressService
        techTreeProgressService;

    private readonly IUserResearchMutationRepository
        researchMutationRepository;

    public UserTechTreeResearchService(
        IUserTechTreeProgressService techTreeProgressService,
        IUserResearchMutationRepository
            researchMutationRepository)
    {
        this.techTreeProgressService =
            techTreeProgressService;

        this.researchMutationRepository =
            researchMutationRepository;
    }

    public async Task<UserTechTreeProgressSnapshot>
        SelectResearchTargetAsync(
            Guid userId,
            string techTreeId,
            string nodeId,
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

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException(
                "A research node ID is required.",
                nameof(nodeId));
        }

        string normalizedTechTreeId = techTreeId.Trim();
        string normalizedNodeId = nodeId.Trim();

        UserTechTreeProgressSnapshot currentSnapshot =
            await techTreeProgressService.GetProgressAsync(
                userId,
                normalizedTechTreeId,
                cancellationToken);

        UserTechTreeNodeProgress? targetNode =
            currentSnapshot.Nodes.SingleOrDefault(node =>
                string.Equals(
                    node.NodeId,
                    normalizedNodeId,
                    StringComparison.Ordinal));

        if (targetNode is null)
        {
            throw new ArgumentException(
                $"Node '{normalizedNodeId}' does not belong to " +
                $"tech tree '{normalizedTechTreeId}'.",
                nameof(nodeId));
        }

        if (targetNode.ResearchState
            == TechTreeNodeResearchState.Locked)
        {
            throw new ResearchTargetNotAvailableException(
                normalizedTechTreeId,
                normalizedNodeId,
                targetNode.ResearchState);
        }

        if (targetNode.ResearchState
            == TechTreeNodeResearchState.Researched)
        {
            throw new ResearchTargetNotAvailableException(
                normalizedTechTreeId,
                normalizedNodeId,
                targetNode.ResearchState);
        }

        if (targetNode.ResearchState
            is not TechTreeNodeResearchState.Available
            and not TechTreeNodeResearchState.Researching)
        {
            throw new ResearchTargetNotAvailableException(
                normalizedTechTreeId,
                normalizedNodeId,
                targetNode.ResearchState);
        }

        UserTechTreeState? currentState =
            await researchMutationRepository
                .FindTechTreeStateForUpdateAsync(
                    userId,
                    normalizedTechTreeId,
                    cancellationToken);

        if (currentState != null &&
            string.Equals(
                currentState.SelectedNodeId,
                normalizedNodeId,
                StringComparison.Ordinal))
        {
            return currentSnapshot;
        }

        DateTime utcNow = DateTime.UtcNow;

        if (currentState is null)
        {
            currentState = UserTechTreeState.Create(
                userId,
                normalizedTechTreeId,
                utcNow);

            currentState.SelectNode(
                normalizedNodeId,
                utcNow);

            researchMutationRepository.AddTechTreeState(
                currentState);
        }
        else
        {
            currentState.SelectNode(
                normalizedNodeId,
                utcNow);
        }

        await researchMutationRepository.SaveChangesAsync(
            cancellationToken);

        return await techTreeProgressService.GetProgressAsync(
            userId,
            normalizedTechTreeId,
            cancellationToken);
    }
}