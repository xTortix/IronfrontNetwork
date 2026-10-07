namespace Ironfront.Shared.Contracts.Auth;

public sealed class RefreshTokenResponse
{
    public Guid SessionId { get; init; }

    public string AccessToken { get; init; } = "";
    public DateTime AccessTokenExpiresAtUtc { get; init; }

    public string RefreshToken { get; init; } = "";
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}