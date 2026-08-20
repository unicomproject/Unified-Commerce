using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

public static class ProductVariantCombinationHashHelper
{
    /// <summary>
    /// Generates the canonical hash for a ProductVariant combination.
    /// Sorts pairs by ProductOptionId, serializes to opt:{Id}|val:{Id}; and computes SHA-256.
    /// </summary>
    public static string GenerateCanonicalHash(IEnumerable<(Guid ProductOptionId, Guid ProductOptionValueId)> pairs)
    {
        var sortedPairs = pairs
            .OrderBy(p => p.ProductOptionId.ToString("D"))
            .ToList();

        var stringPairs = sortedPairs.Select(pair => $"opt:{pair.ProductOptionId:D}|val:{pair.ProductOptionValueId:D}");
        var canonicalString = string.Join(";", stringPairs);
        var bytes = Encoding.UTF8.GetBytes(canonicalString);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
