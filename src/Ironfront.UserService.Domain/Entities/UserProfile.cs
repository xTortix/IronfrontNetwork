namespace Ironfront.UserService.Domain.Entities;

public sealed class UserProfile
{
    private UserProfile()
    {
    }

    public Guid UserId { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static UserProfile Create(
        Guid userId,
        string username,
        string email,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new UserProfile
        {
            UserId = userId,
            Username = username.Trim(),
            Email = email.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool SynchronizeIdentity(
        string username,
        string email,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        string normalizedUsername = username.Trim();
        string normalizedEmail = email.Trim();

        bool changed =
            !string.Equals(
                Username,
                normalizedUsername,
                StringComparison.Ordinal)
            || !string.Equals(
                Email,
                normalizedEmail,
                StringComparison.Ordinal);

        if (!changed)
        {
            return false;
        }

        Username = normalizedUsername;
        Email = normalizedEmail;
        UpdatedAtUtc = utcNow;

        return true;
    }
}