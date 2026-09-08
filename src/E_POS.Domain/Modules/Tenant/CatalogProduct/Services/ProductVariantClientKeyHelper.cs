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

    /// <summary>
    /// Name-based client key used by legacy wizard payloads before template IDs are bound.
    /// Example: Color:Red;Size:Small
    /// </summary>
    public static string BuildNameBasedClientCombinationKey(IEnumerable<(string OptionName, string ValueName)> pairs)
    {
        return string.Join(";", pairs
            .OrderBy(p => p.OptionName, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p.OptionName}:{p.ValueName}"));
    }
}
