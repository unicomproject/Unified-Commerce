using System.Security.Cryptography;
using System.Text;

namespace E_POS.Application.Modules.Tenant.HardwareCash;

/// <summary>
/// Deterministic drawer request IDs so checkout/return retries reuse one operation intent.
/// </summary>
public static class CashDrawerStableRequestId
{
    public static Guid ForBusinessReference(Guid businessReferenceId, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var material = $"{businessReferenceId:N}:{purpose.Trim().ToLowerInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        // UUID version 4 + RFC 4122 variant bits (stable, non-random payload).
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
