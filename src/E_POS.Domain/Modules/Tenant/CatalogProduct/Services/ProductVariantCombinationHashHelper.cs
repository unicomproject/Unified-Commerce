using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

public static class ProductVariantCombinationHashHelper
{
    /// <summary>
    /// Generates the canonical persisted hash for a ProductVariant combination.
    /// Sorts pairs by ProductOptionId, serializes to opt:{Id}|val:{Id}; and computes SHA-256.
    /// </summary>
    public static string GenerateCanonicalHash(IEnumerable<(Guid ProductOptionId, Guid ProductOptionValueId)> pairs)
    {
        var sortedPairs = pairs
            .OrderBy(p => p.ProductOptionId.ToString("D"))
            .ToList();

        var stringPairs = sortedPairs.Select(pair => $"opt:{pair.ProductOptionId:D}|val:{pair.ProductOptionValueId:D}");
        var canonicalString = string.Join(";", stringPairs);
        return ComputeSha256Hex(canonicalString);
    }

    /// <summary>
    /// Legacy Phase-1 preview hash (MD5, 32 hex chars). Used only for backward-compatible reconciliation
    /// of drafts saved before canonical SHA-256 persistence was enforced.
    /// </summary>
    public static string GenerateLegacyMd5PreviewHash(
        IEnumerable<(Guid? SourceOptionTemplateId, Guid? SourceOptionTemplateValueId, string? OptionName, string? ValueName)> orderedValues)
    {
        var hashInput = string.Join("|", orderedValues.Select(x =>
            $"{x.SourceOptionTemplateId?.ToString("D") ?? x.OptionName}:{x.SourceOptionTemplateValueId?.ToString("D") ?? x.ValueName}"));
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool IsLegacyMd5Hash(string? hash) =>
        !string.IsNullOrWhiteSpace(hash) && hash.Length == 32;

    public static bool IsCanonicalSha256Hash(string? hash) =>
        !string.IsNullOrWhiteSpace(hash) && hash.Length == 64;

    public static bool MatchesCombinationHash(
        string? storedHash,
        string? candidateHash,
        string? legacyMd5PreviewHash = null)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(candidateHash) &&
            string.Equals(storedHash, candidateHash, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(legacyMd5PreviewHash) &&
               IsLegacyMd5Hash(storedHash) &&
               string.Equals(storedHash, legacyMd5PreviewHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256Hex(string canonicalString)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalString);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
