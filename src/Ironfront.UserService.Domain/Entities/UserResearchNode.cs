namespace Ironfront.UserService.Domain.Entities;

public sealed class UserResearchNode
{
    private const int MaxNodeIdLength = 128;

    private UserResearchNode()
    {
    }

    public Guid UserId { get; private set; }

    public string NodeId { get; private set; } = string.Empty;

    public long ResearchPointsApplied { get; private set; }

    public DateTime? ResearchStartedAtUtc { get; private set; }

    public DateTime? ResearchedAtUtc { get; private set; }

    public DateTime? PurchasedAtUtc { get; private set; }

    public static UserResearchNode Create(
        Guid userId,
        string nodeId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        string normalizedNodeId = nodeId.Trim();

        if (normalizedNodeId.Length > MaxNodeIdLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeId),
                $"Node ID must not exceed {MaxNodeIdLength} characters.");
        }

        return new UserResearchNode
        {
            UserId = userId,
            NodeId = normalizedNodeId,
            ResearchPointsApplied = 0
        };
    }

    public void ApplyResearchPoints(
        long researchPoints,
        long requiredResearchPoints,
        DateTime utcNow)
    {
        if (researchPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(researchPoints),
                "Research points must be greater than zero.");
        }

        if (requiredResearchPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredResearchPoints),
                "Required research points must not be negative.");
        }

        if (ResearchedAtUtc is not null)
        {
            throw new InvalidOperationException(
                $"Research node '{NodeId}' is already researched.");
        }

        ResearchStartedAtUtc ??= utcNow;

        if (requiredResearchPoints == 0)
        {
            ResearchPointsApplied = 0;
            ResearchedAtUtc = utcNow;
            return;
        }

        long totalResearchPoints = checked(
            ResearchPointsApplied + researchPoints);

        ResearchPointsApplied = Math.Min(
            totalResearchPoints,
            requiredResearchPoints);

        if (ResearchPointsApplied >= requiredResearchPoints)
        {
            ResearchedAtUtc = utcNow;
        }
    }

    public void MarkResearched(
        long requiredResearchPoints,
        DateTime utcNow)
    {
        if (requiredResearchPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredResearchPoints),
                "Required research points must not be negative.");
        }

        ResearchStartedAtUtc ??= utcNow;
        ResearchPointsApplied = requiredResearchPoints;
        ResearchedAtUtc ??= utcNow;
    }

    public void MarkPurchased(DateTime utcNow)
    {
        if (ResearchedAtUtc is null)
        {
            throw new InvalidOperationException(
                $"Research node '{NodeId}' must be researched before it can be purchased.");
        }

        PurchasedAtUtc ??= utcNow;
    }
}