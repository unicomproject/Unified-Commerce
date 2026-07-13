using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.Discount.Services;

public static class PosDiscountCartFingerprint
{
    public static string CreateSnapshotJson(
        Guid deviceId,
        string? saleType,
        Guid? customerId,
        IReadOnlyList<PosCheckoutLineRequestDto> lines,
        int subtotal,
        string currencyCode)
    {
        var canonicalLines = lines
            .Where(x => x.VariantId != Guid.Empty && x.Qty > 0)
            .GroupBy(x => x.VariantId)
            .Select(x => new { variantId = x.Key, qty = x.Sum(y => y.Qty) })
            .OrderBy(x => x.variantId)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            deviceId,
            saleType = string.IsNullOrWhiteSpace(saleType) ? "NewSale" : saleType.Trim(),
            customerId,
            lines = canonicalLines,
            subtotal,
            currencyCode = currencyCode.Trim().ToUpperInvariant()
        });
    }

    public static string Hash(string snapshotJson) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson))).ToLowerInvariant();
}
