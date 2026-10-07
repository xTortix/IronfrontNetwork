namespace Ironfront.UserService.Domain.Entities;

public sealed class UserTechTreeState
{
    private const int MaxTechTreeIdLength = 128;
    private const int MaxNodeIdLength = 128;

    private UserTechTreeState()
    {
    }

    public Guid UserId { get; private set; }

    public string TechTreeId { get; private set; } = string.Empty;

    public string? SelectedNodeId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static UserTechTreeState Create(
        Guid userId,
        string techTreeId,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        return new UserTechTreeState
        {
            UserId = userId,
            TechTreeId = NormalizeRequiredId(
                techTreeId,
                nameof(techTreeId),
                MaxTechTreeIdLength),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool SelectNode(
        string nodeId,
        DateTime utcNow)
    {
        string normalizedNodeId = NormalizeRequiredId(
            nodeId,
            nameof(nodeId),
            MaxNodeIdLength);

        if (string.Equals(
                SelectedNodeId,
                normalizedNodeId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        SelectedNodeId = normalizedNodeId;
        UpdatedAtUtc = utcNow;

        return true;
    }

    public bool ClearSelectedNode(DateTime utcNow)
    {
        if (SelectedNodeId is null)
        {
            return false;
        }

        SelectedNodeId = null;
        UpdatedAtUtc = utcNow;

        return true;
    }

    private static string NormalizeRequiredId(
        string value,
        string parameterName,
        int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value must not exceed {maxLength} characters.");
        }

        return normalizedValue;
    }
}