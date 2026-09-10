using System.Text.RegularExpressions;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

/// <summary>
/// Canonical barcode type codes and format validation for Product Setup Step 5.
/// Server validation is authoritative; clients may mirror for UX only.
/// </summary>
public static class ProductBarcodeFormatValidator
{
    public const string Ean13 = "EAN13";
    public const string Ean8 = "EAN8";
    public const string UpcA = "UPCA";
    public const string Code128 = "CODE128";
    public const string Code39 = "CODE39";

    public static readonly IReadOnlyList<(string Code, string Label)> CanonicalTypes =
    [
        (Ean13, "EAN-13"),
        (Ean8, "EAN-8"),
        (UpcA, "UPC-A"),
        (Code128, "CODE-128"),
        (Code39, "CODE-39"),
    ];

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Ean13, Ean8, UpcA, Code128, Code39,
    };

    private static readonly Regex Code39Pattern = new(
        @"^[0-9A-Z\-.\s$/+%]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Code128Pattern = new(
        @"^[\x20-\x7E]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? NormalizeType(string? barcodeType)
    {
        if (string.IsNullOrWhiteSpace(barcodeType))
        {
            return null;
        }

        var normalized = barcodeType.Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return AllowedTypes.Contains(normalized) ? normalized : null;
    }

    public static bool IsKnownType(string? barcodeType) =>
        NormalizeType(barcodeType) is not null;

    /// <summary>
    /// Validates barcode value for the selected type.
    /// Blank barcode is not validated here (optional under Step 5 contract).
    /// </summary>
    public static string? Validate(string? barcode, string? barcodeType)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        // Preserve leading zeros — never numeric-cast. Use the raw trimmed string.
        var value = barcode.Trim();
        if (value.Length > 100)
        {
            return "Barcode cannot exceed 100 characters.";
        }

        var type = NormalizeType(barcodeType);
        if (type is null)
        {
            return "Barcode type is required when a barcode is provided.";
        }

        return type switch
        {
            Ean13 => ValidateEan(value, 13),
            Ean8 => ValidateEan(value, 8),
            UpcA => ValidateEan(value, 12),
            Code128 => Code128Pattern.IsMatch(value)
                ? null
                : "CODE128 barcode contains invalid characters.",
            Code39 => Code39Pattern.IsMatch(value)
                ? null
                : "CODE39 barcode contains invalid characters.",
            _ => "Unsupported barcode type.",
        };
    }

    private static string? ValidateEan(string value, int expectedLength)
    {
        if (value.Length != expectedLength)
        {
            return $"Barcode must be exactly {expectedLength} digits for the selected type.";
        }

        if (!value.All(char.IsDigit))
        {
            return "Barcode must contain only digits for the selected type.";
        }

        if (!PassesGtinChecksum(value))
        {
            return "Barcode checksum is invalid.";
        }

        return null;
    }

    /// <summary>
    /// GS1 / GTIN mod-10 checksum (works for EAN-8, UPC-A, EAN-13).
    /// </summary>
    private static bool PassesGtinChecksum(string digits)
    {
        var sum = 0;
        var multiplyByThree = true;
        for (var i = digits.Length - 2; i >= 0; i--)
        {
            var n = digits[i] - '0';
            sum += multiplyByThree ? n * 3 : n;
            multiplyByThree = !multiplyByThree;
        }

        var check = (10 - (sum % 10)) % 10;
        return check == digits[^1] - '0';
    }
}
