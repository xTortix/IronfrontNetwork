namespace Ironfront.UserService.Application.Models;

public enum TechTreeNodeResearchState : byte
{
    Locked = 0,
    Available = 1,
    Researching = 2,
    Researched = 3
}

public sealed record UserTechTreeProgressSnapshot(
    Guid UserId,
    string TechTreeId,
    string? SelectedNodeId,
    IReadOnlyList<UserTechTreeNodeProgress> Nodes);

public sealed record UserTechTreeNodeProgress(
    string NodeId,
    TechTreeNodeResearchState ResearchState,
    long ResearchPointsApplied,
    DateTime? ResearchStartedAtUtc,
    DateTime? ResearchedAtUtc,
    DateTime? PurchasedAtUtc,
    bool IsVehicleOwned);