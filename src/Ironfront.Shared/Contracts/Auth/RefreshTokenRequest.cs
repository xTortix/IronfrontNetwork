namespace Ironfront.Shared.Contracts.Auth;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = "";
}