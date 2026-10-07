namespace Ironfront.Shared.Contracts.Auth;

public sealed class LogoutRequest
{
    public string RefreshToken { get; init; } = "";
}