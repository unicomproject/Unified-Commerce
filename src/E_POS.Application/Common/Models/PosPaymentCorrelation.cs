using System.Security.Cryptography;
using System.Text;

namespace E_POS.Application.Common.Models;

public static class PosPaymentCorrelation
{
    public static string FromIdempotencyKey(string? idempotencyKey, string? fallback = null)
    {
        var source = string.IsNullOrWhiteSpace(idempotencyKey) ? fallback : idempotencyKey;
        if (string.IsNullOrWhiteSpace(source))
        {
            source = Guid.NewGuid().ToString("N");
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))
            [..12]
            .ToLowerInvariant();
    }
}
