namespace Ironfront.Shared.Contracts.UserService;

public sealed record ProvisionUserRequest(
    Guid UserId,
    string Username,
    string Email);
