namespace Ironfront.Shared.Contracts.UserService;

public sealed record ProvisionUserResponse(
    Guid UserId,
    string Username,
    string Email,
    Guid ActiveDeckId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool WasCreated);