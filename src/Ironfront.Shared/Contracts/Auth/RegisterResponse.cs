namespace Ironfront.Shared.Contracts.Auth;

public sealed class RegisterResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTime CreatedAtUtc { get; init; }
}