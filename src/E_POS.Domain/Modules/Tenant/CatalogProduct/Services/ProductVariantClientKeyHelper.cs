using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

public static class ProductVariantClientKeyHelper
{
    /// <summary>
    /// Generates the canonical clientCombinationKey.
    /// Used before persistence to identify incoming client combinations.
    /// Sorts pairs by sourceOptionTemplateId, serializes to {Id}:{Id}; (NO hashing).
    /// </summary>
    public static string GenerateClientCombinationKey(IEnumerable<(Guid SourceOptionTemplateId, Guid SourceOptionTemplateValueId)> pairs)
    {
        var sortedPairs = pairs
            .OrderBy(p => p.SourceOptionTemplateId.ToString("D"))
            .ToList();

        var stringPairs = sortedPairs.Select(pair => $"{pair.SourceOptionTemplateId:D}:{pair.SourceOptionTemplateValueId:D}");
        return string.Join(";", stringPairs);
    }
}
