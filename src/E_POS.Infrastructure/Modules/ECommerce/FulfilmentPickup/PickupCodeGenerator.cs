using System.Security.Cryptography;
using System.Text;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup;

public static class PickupCodeGenerator
{
    private const int CodeByteLength = 20;

    public static string Generate() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(CodeByteLength));

    public static bool Matches(string? storedCode, string? providedCode)
    {
        if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(providedCode))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedCode),
            Encoding.UTF8.GetBytes(providedCode));
    }
}
