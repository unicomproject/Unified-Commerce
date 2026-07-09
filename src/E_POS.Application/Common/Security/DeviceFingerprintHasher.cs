using System.Security.Cryptography;
using System.Text;

namespace E_POS.Application.Common.Security;

public static class DeviceFingerprintHasher
{
    public static string Hash(string fingerprint)
    {
        var normalized = fingerprint.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
