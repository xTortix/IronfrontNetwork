namespace Ironfront.Shared.Contracts.UserService;

public sealed record UserTechTreeProgressResponse(
    Guid UserId,
    string TechTreeId,
    string? SelectedNodeId,
    IReadOnlyList<UserTechTreeNodeProgressResponse> Nodes);

public sealed record UserTechTreeNodeProgressResponse(
    string NodeId,
    string ResearchState,
    long ResearchPointsApplied,
    DateTime? ResearchStartedAtUtc,
    DateTime? ResearchedAtUtc,
    DateTime? PurchasedAtUtc,
    bool IsVehicleOwned);