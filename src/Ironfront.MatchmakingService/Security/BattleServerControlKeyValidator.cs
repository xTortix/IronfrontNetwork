using System.Security.Cryptography;
using System.Text;

namespace Ironfront.MatchmakingService.Security;

internal static class BattleServerControlKeyValidator
{
    private const string HeaderName =
        "X-Ironfront-Battle-Server-Key";

    public static bool IsValid(
        HttpRequest request,
        string expectedKey)
    {
        if (!request.Headers.TryGetValue(
                HeaderName,
                out var providedValues))
        {
            return false;
        }

        string providedKey = providedValues.ToString();

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return false;
        }

        byte[] expectedHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(expectedKey));

        byte[] providedHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(providedKey));

        return CryptographicOperations.FixedTimeEquals(
            expectedHash,
            providedHash);
    }
}