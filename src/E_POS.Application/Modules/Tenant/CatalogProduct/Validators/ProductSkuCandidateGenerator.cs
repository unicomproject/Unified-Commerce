namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

/// <summary>
/// Product-type-aware AUTO SKU formatting. Sequence allocation and persistence
/// are orchestrated outside this formatter. Final ownership remains product_variants.sku.
/// </summary>
public static class ProductSkuCandidateGenerator
{
    public const string NoBarcodeProductPurpose = "NO_BARCODE_PRODUCT";
    public const string AutoMode = "AUTO";
    public const int MaxLength = 100;
    public const int MaxAttempts = 32;
    public const int SequenceWidth = 6;

    /// <summary>
    /// Builds one stable Product base from persisted CategoryCode and the
    /// tenant-wide Product sequence.
    /// </summary>
    public static string BuildProductBase(string categoryCode, long sequence)
    {
        var categoryToken = NormalizeToken(categoryCode, nameof(categoryCode));
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be positive.");
        }

        var candidate = $"{categoryToken}-{sequence.ToString($"D{SequenceWidth}")}";
        EnsureValidLength(candidate);
        return candidate;
    }

    /// <summary>
    /// Extends a Product base with stable value codes in canonical option order.
    /// </summary>
    public static string BuildVariantSku(string productBase, IEnumerable<string> orderedValueCodes)
    {
        var normalizedBase = NormalizeSku(productBase, nameof(productBase));
        var suffixes = orderedValueCodes
            .Select(code => NormalizeToken(code, nameof(orderedValueCodes)))
            .ToArray();
        if (suffixes.Length == 0)
        {
            throw new ArgumentException("At least one Variant Value code is required.", nameof(orderedValueCodes));
        }

        var sku = $"{normalizedBase}-{string.Join("-", suffixes)}";
        EnsureValidLength(sku);
        return sku;
    }

    public static bool MatchesCategory(string productBase, string categoryCode)
    {
        var normalizedBase = NormalizeSku(productBase, nameof(productBase));
        var categoryToken = NormalizeToken(categoryCode, nameof(categoryCode));
        return normalizedBase.StartsWith($"{categoryToken}-", StringComparison.Ordinal);
    }

    private static string NormalizeToken(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SKU token is required.", parameterName);
        }

        var token = value.Trim().ToUpperInvariant();
        if (token.StartsWith('-') ||
            token.EndsWith('-') ||
            token.Contains("--", StringComparison.Ordinal) ||
            token.Any(ch => !(ch is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')))
        {
            throw new ArgumentException("SKU token contains unsupported characters.", parameterName);
        }

        return token;
    }

    private static string NormalizeSku(string? value, string parameterName)
    {
        var sku = NormalizeToken(value, parameterName);
        EnsureValidLength(sku);
        return sku;
    }

    private static void EnsureValidLength(string value)
    {
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Generated SKU exceeds {MaxLength} characters.");
        }
    }
}
