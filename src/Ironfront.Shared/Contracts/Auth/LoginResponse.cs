namespace Ironfront.Shared.Contracts.Auth;

public sealed class LoginResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTime CreatedAtUtc { get; init; }

    // Identifies one refresh-token session / device login.
    public Guid SessionId { get; init; }

    // Short-lived JWT used for normal authenticated API requests.
    public string AccessToken { get; init; } = "";
    public DateTime AccessTokenExpiresAtUtc { get; init; }

    // Long-lived opaque token used only to obtain a new access token.
    public string RefreshToken { get; init; } = "";
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}