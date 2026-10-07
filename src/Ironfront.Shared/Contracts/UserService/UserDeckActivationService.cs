namespace Ironfront.Shared.Contracts.UserService;

public sealed record UserDeckActivationResponse(
    Guid UserId,
    Guid DeckId,
    string Name,
    string Nation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);