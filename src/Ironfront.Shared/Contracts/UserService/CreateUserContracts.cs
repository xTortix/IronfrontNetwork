namespace Ironfront.Shared.Contracts.UserService;

public sealed record CreateUserDeckRequest(
    string Name,
    string Nation);

public sealed record CreateUserDeckResponse(
    Guid UserId,
    Guid DeckId,
    string Name,
    string Nation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);