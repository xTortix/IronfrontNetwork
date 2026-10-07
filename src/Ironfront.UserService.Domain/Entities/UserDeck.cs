using Ironfront.UserService.Domain.Rules;

namespace Ironfront.UserService.Domain.Entities;

public sealed class UserDeck
{
    private UserDeck()
    {
    }

    public Guid DeckId { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Nation { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static UserDeck Create(
        Guid userId,
        string name,
        string nation,
        bool isActive,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        return new UserDeck
        {
            DeckId = Guid.NewGuid(),
            UserId = userId,
            Name = NormalizeName(name),
            Nation = NormalizeNation(nation),
            IsActive = isActive,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Rename(
        string name,
        DateTime utcNow)
    {
        Name = NormalizeName(name);
        UpdatedAtUtc = utcNow;
    }

    public void SetActive(
        bool isActive,
        DateTime utcNow)
    {
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string normalizedName = name.Trim();

        if (normalizedName.Length > DeckRules.MaxDeckNameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                $"Deck name must not exceed {DeckRules.MaxDeckNameLength} characters.");
        }

        return normalizedName;
    }

    private static string NormalizeNation(string nation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nation);

        string normalizedNation = nation.Trim();

        if (normalizedNation.Length > DeckRules.MaxNationLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nation),
                $"Nation must not exceed {DeckRules.MaxNationLength} characters.");
        }

        return normalizedNation;
    }
}
