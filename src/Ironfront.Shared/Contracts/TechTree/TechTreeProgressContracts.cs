namespace Ironfront.Shared.Contracts.TechTree;

public sealed record TechTreeProgressResponse(
    string TechTreeId,
    string? SelectedNodeId,
    IReadOnlyList<TechTreeNodeProgressResponse> Nodes);

public sealed record TechTreeNodeProgressResponse(
    string NodeId,
    string ResearchState,
    long ResearchPointsApplied,
    DateTime? ResearchStartedAtUtc,
    DateTime? ResearchedAtUtc,
    DateTime? PurchasedAtUtc,
    bool IsVehicleOwned);