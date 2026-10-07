using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ironfront.AuthService.Persistence.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Ironfront.AuthService.Services;

public sealed class JwtTokenService
{
    private const int MinimumSigningKeyLength = 32;

    private readonly string issuer;
    private readonly string audience;
    private readonly SymmetricSecurityKey signingKey;
    private readonly SigningCredentials signingCredentials;

    public JwtTokenService()
    {
        issuer = Environment.GetEnvironmentVariable("IRONFRONT_JWT_ISSUER")
            ?? "Ironfront.AuthService";

        audience = Environment.GetEnvironmentVariable("IRONFRONT_JWT_AUDIENCE")
            ?? "Ironfront.Client";

        string? signingKeyValue =
            Environment.GetEnvironmentVariable("IRONFRONT_JWT_SIGNING_KEY");

        if (string.IsNullOrWhiteSpace(signingKeyValue))
        {
            throw new InvalidOperationException(
                "Missing environment variable IRONFRONT_JWT_SIGNING_KEY.");
        }

        if (signingKeyValue.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"IRONFRONT_JWT_SIGNING_KEY must be at least {MinimumSigningKeyLength} characters long.");
        }

        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(signingKeyValue);

        signingKey = new SymmetricSecurityKey(signingKeyBytes);

        signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);
    }

    public JwtTokenResult CreateAccessToken(UserEntity user)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime expiresAtUtc = nowUtc.AddHours(2);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("user_id", user.Id.ToString()),
            new Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}

public sealed class JwtTokenResult
{
    public string AccessToken { get; init; } = "";
    public DateTime ExpiresAtUtc { get; init; }
}