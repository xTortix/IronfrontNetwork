using Ironfront.GameData.Application;
using Ironfront.GameData.Domain;
using Ironfront.UserService.Application.Exceptions;
using Ironfront.UserService.Application.Models;
using Ironfront.UserService.Domain.Entities;

namespace Ironfront.UserService.Application.Services;

public sealed class UserTechTreeProgressService
    : IUserTechTreeProgressService
{
    private readonly IUserResearchRepository researchRepository;
    private readonly ITechTreeCatalog techTreeCatalog;

    public UserTechTreeProgressService(
        IUserResearchRepository researchRepository,
        ITechTreeCatalog techTreeCatalog)
    {
        this.researchRepository = researchRepository;
        this.techTreeCatalog = techTreeCatalog;
    }

    public async Task<UserTechTreeProgressSnapshot> GetProgressAsync(
        Guid userId,
        string techTreeId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(techTreeId);

        string normalizedTechTreeId = techTreeId.Trim();

        if (!techTreeCatalog.TryGetTechTree(
                normalizedTechTreeId,
                out TechTreeCatalogEntry? techTree)
            || techTree is null)
        {
            throw new TechTreeNotFoundException(normalizedTechTreeId);
        }

        bool profileExists = await researchRepository.ProfileExistsAsync(
            userId,
            cancellationToken);

        if (!profileExists)
        {
            throw new UserProfileNotFoundException(userId);
        }

        UserTechTreeState? storedTreeState =
            await researchRepository.FindTechTreeStateAsync(
                userId,
                techTree.TechTreeId,
                cancellationToken);

        IReadOnlyList<UserResearchNode> storedResearchNodes =
            await researchRepository.GetResearchNodesAsync(
                userId,
                techTree.Nodes
                    .Select(node => node.NodeId)
                    .ToArray(),
                cancellationToken);

        IReadOnlyList<string> ownedVehicleIds =
            await researchRepository.GetOwnedVehicleIdsAsync(
                userId,
                cancellationToken);

        string? selectedNodeId = storedTreeState?.SelectedNodeId;

        if (!string.IsNullOrWhiteSpace(selectedNodeId)
            && !techTree.TryGetNode(
                selectedNodeId,
                out _))
        {
            throw new InvalidOperationException(
                $"User '{userId}' selected node '{selectedNodeId}' in tech tree '{techTree.TechTreeId}', but that node no longer exists in the active catalog.");
        }

        Dictionary<string, UserResearchNode> researchNodeById =
            storedResearchNodes.ToDictionary(
                node => node.NodeId,
                StringComparer.Ordinal);

        HashSet<string> ownedVehicleIdSet = new(
            ownedVehicleIds,
            StringComparer.Ordinal);

        IReadOnlyDictionary<string, TechTreeNodeResearchState>
            researchStateByNodeId = CalculateResearchStates(
                techTree,
                researchNodeById,
                selectedNodeId);

        var nodeProgress = new List<UserTechTreeNodeProgress>(
            techTree.Nodes.Count);

        foreach (TechTreeNodeCatalogEntry node in techTree.Nodes)
        {
            researchNodeById.TryGetValue(
                node.NodeId,
                out UserResearchNode? storedNode);

            nodeProgress.Add(new UserTechTreeNodeProgress(
                node.NodeId,
                researchStateByNodeId[node.NodeId],
                storedNode?.ResearchPointsApplied ?? 0,
                storedNode?.ResearchStartedAtUtc,
                storedNode?.ResearchedAtUtc,
                storedNode?.PurchasedAtUtc,
                ownedVehicleIdSet.Contains(node.VehicleId)));
        }

        return new UserTechTreeProgressSnapshot(
            userId,
            techTree.TechTreeId,
            selectedNodeId,
            nodeProgress);
    }

    private static IReadOnlyDictionary<string, TechTreeNodeResearchState>
        CalculateResearchStates(
            TechTreeCatalogEntry techTree,
            IReadOnlyDictionary<string, UserResearchNode> researchNodeById,
            string? selectedNodeId)
    {
        Dictionary<string, TechTreeNodeCatalogEntry> nodesById =
            techTree.Nodes.ToDictionary(
                node => node.NodeId,
                StringComparer.Ordinal);

        var resolvedStates =
            new Dictionary<string, TechTreeNodeResearchState>(
                StringComparer.Ordinal);

        foreach (TechTreeNodeCatalogEntry node in techTree.Nodes)
        {
            ResolveState(node);
        }

        if (!string.IsNullOrWhiteSpace(selectedNodeId)
            && resolvedStates.TryGetValue(
                selectedNodeId,
                out TechTreeNodeResearchState selectedState)
            && selectedState != TechTreeNodeResearchState.Researching)
        {
            throw new InvalidOperationException(
                $"Selected node '{selectedNodeId}' is not currently researchable.");
        }

        return resolvedStates;

        TechTreeNodeResearchState ResolveState(
            TechTreeNodeCatalogEntry node)
        {
            if (resolvedStates.TryGetValue(
                    node.NodeId,
                    out TechTreeNodeResearchState existingState))
            {
                return existingState;
            }

            bool prerequisitesResearched = true;

            foreach (string requiredNodeId in node.RequiredNodeIds)
            {
                TechTreeNodeCatalogEntry requiredNode =
                    nodesById[requiredNodeId];

                if (ResolveState(requiredNode)
                    != TechTreeNodeResearchState.Researched)
                {
                    prerequisitesResearched = false;
                    break;
                }
            }

            if (!prerequisitesResearched)
            {
                resolvedStates[node.NodeId] =
                    TechTreeNodeResearchState.Locked;

                return TechTreeNodeResearchState.Locked;
            }

            researchNodeById.TryGetValue(
                node.NodeId,
                out UserResearchNode? storedNode);

            if (storedNode is not null)
            {
                if (storedNode.ResearchPointsApplied
                    > node.ResearchPointCost)
                {
                    throw new InvalidOperationException(
                        $"User research node '{node.NodeId}' has more applied RP than the active catalog allows.");
                }

                if (storedNode.ResearchedAtUtc is null
                    && storedNode.ResearchPointsApplied
                    >= node.ResearchPointCost
                    && node.ResearchPointCost > 0)
                {
                    throw new InvalidOperationException(
                        $"User research node '{node.NodeId}' reached its RP requirement but has no researched timestamp.");
                }
            }

            bool isResearched =
                node.ResearchPointCost == 0
                || storedNode?.ResearchedAtUtc is not null;

            if (isResearched)
            {
                resolvedStates[node.NodeId] =
                    TechTreeNodeResearchState.Researched;

                return TechTreeNodeResearchState.Researched;
            }

            bool isSelectedForResearch =
                string.Equals(
                    selectedNodeId,
                    node.NodeId,
                    StringComparison.Ordinal);

            TechTreeNodeResearchState state =
                isSelectedForResearch
                    ? TechTreeNodeResearchState.Researching
                    : TechTreeNodeResearchState.Available;

            resolvedStates[node.NodeId] = state;

            return state;
        }
    }
}