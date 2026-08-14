using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

public static class ProductVariantCodeGenerator
{
    private static readonly Regex _alphanumericRegex = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);

    /// <summary>
    /// Generates a deterministic VariantCode.
    /// Handles prefix normalization, hash suffixing, and collision resolution.
    /// </summary>
    public static string GenerateVariantCode(
        string? productCode,
        Guid productId,
        string optionCombinationHash,
        Func<string, bool> checkCollision)
    {
        string safePrefix;
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var upper = productCode.ToUpperInvariant();
            var alphanumeric = new string(upper.Where(char.IsLetterOrDigit).ToArray());
            if (alphanumeric.Length > 0)
            {
                safePrefix = alphanumeric.Length > 15 ? alphanumeric[..15] : alphanumeric;
            }
            else
            {
                safePrefix = productId.ToString("N")[..8].ToUpperInvariant();
            }
        }
        else
        {
            safePrefix = productId.ToString("N")[..8].ToUpperInvariant();
        }

        var fullHash = optionCombinationHash.ToUpperInvariant();
        
        // Step 1: 8 chars
        var suffix = fullHash[..8];
        var code = $"VAR-{safePrefix}-{suffix}";
        if (!checkCollision(code))
        {
            return code;
        }

        // Step 2: 12 chars
        suffix = fullHash[..12];
        code = $"VAR-{safePrefix}-{suffix}";
        if (!checkCollision(code))
        {
            return code;
        }

        // Step 3: 16 chars
        suffix = fullHash[..16];
        code = $"VAR-{safePrefix}-{suffix}";
        if (!checkCollision(code))
        {
            return code;
        }

        // Terminal failure
        throw new InvalidOperationException($"Unable to resolve VariantCode collision for Product {productId} after maximum attempts.");
    }
}
