namespace Ironfront.Shared.Contracts.Auth;

public sealed class RegisterRequest
{
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
}