using System.Security.Cryptography;
using System.Text;

namespace Ironfront.UserService.Security;

internal static class InternalServiceKeyValidator
{
    public static bool IsValid(
        HttpRequest request,
        string expectedKey)
    {
        if (!request.Headers.TryGetValue(
            "X-Ironfront-Internal-Key",
            out var providedValues))
        {
            return false;
        }

        string providedKey = providedValues.ToString();

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return false;
        }

        byte[] expectedHash =
        SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));

        byte[] providedHash =
        SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));

        return CryptographicOperations.FixedTimeEquals(
            expectedHash,
            providedHash);
    }
}
