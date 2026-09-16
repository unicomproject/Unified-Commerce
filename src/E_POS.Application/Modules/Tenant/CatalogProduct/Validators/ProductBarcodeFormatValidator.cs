using System.Text.RegularExpressions;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed record ProductIdentifierValidationResult(
    string? OriginalIdentifier,
    string NormalizedIdentifier,
    bool IsValid,
    string? IdentifierStandard,
    bool CheckDigitApplicable,
    bool CheckDigitValid,
    string BarcodeType,
    string? ReportedSymbology,
    string? FailureReason);

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
    public const string Unknown = "UNKNOWN";

    public const string Gtin8 = "GTIN8";
    public const string Gtin12 = "GTIN12";
    public const string Gtin13 = "GTIN13";
    public const string Gtin14 = "GTIN14";
    public const string Other = "OTHER";

    public const string IdentifierStandardGtin8 = Gtin8;
    public const string IdentifierStandardGtin12 = Gtin12;
    public const string IdentifierStandardGtin13 = Gtin13;
    public const string IdentifierStandardGtin14 = Gtin14;
    public const string IdentifierStandardOther = Other;

    public const string EmptyFailureReason = "EMPTY";
    public const string LengthNotSupportedFailureReason = "LENGTH_NOT_SUPPORTED";
    public const string ChecksumFailedFailureReason = "CHECKSUM_FAILED";
    public const string NonNumericGtinFailureReason = "NON_NUMERIC_GTIN";

    public const string FailureReasonEmpty = EmptyFailureReason;
    public const string FailureReasonLengthNotSupported = LengthNotSupportedFailureReason;
    public const string FailureReasonChecksumFailed = ChecksumFailedFailureReason;
    public const string FailureReasonNonNumericGtin = NonNumericGtinFailureReason;

    public static readonly IReadOnlyList<(string Code, string Label)> CanonicalTypes =
    [
        (Ean13, "EAN-13"),
        (Ean8, "EAN-8"),
        (UpcA, "UPC-A"),
        (Code128, "CODE-128"),
        (Code39, "CODE-39"),
        (Unknown, "Unknown"),
    ];

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Ean13, Ean8, UpcA, Code128, Code39, Unknown,
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

    public static ProductIdentifierValidationResult Classify(
        string? identifier,
        string? reportedSymbology = null)
    {
        var normalizedIdentifier = identifier?.Trim() ?? string.Empty;
        var normalizedSymbology = NormalizeType(reportedSymbology);
        var barcodeType = normalizedSymbology ?? Unknown;

        if (normalizedIdentifier.Length == 0)
        {
            return Result(false, null, false, false, EmptyFailureReason);
        }

        if (normalizedIdentifier.All(char.IsDigit))
        {
            var standard = normalizedIdentifier.Length switch
            {
                8 => Gtin8,
                12 => Gtin12,
                13 => Gtin13,
                14 => Gtin14,
                _ => null,
            };

            if (standard is null)
            {
                return Result(false, null, false, false, LengthNotSupportedFailureReason);
            }

            var checksumValid = PassesGtinChecksum(normalizedIdentifier);
            return Result(
                checksumValid,
                standard,
                true,
                checksumValid,
                checksumValid ? null : ChecksumFailedFailureReason);
        }

        if (normalizedSymbology is Ean13 or Ean8 or UpcA)
        {
            return Result(false, null, true, false, NonNumericGtinFailureReason);
        }

        if (normalizedSymbology == Code128)
        {
            var valid = Code128Pattern.IsMatch(normalizedIdentifier);
            return Result(valid, Other, false, false, valid ? null : LengthNotSupportedFailureReason);
        }

        if (normalizedSymbology == Code39)
        {
            var valid = Code39Pattern.IsMatch(normalizedIdentifier);
            return Result(valid, Other, false, false, valid ? null : LengthNotSupportedFailureReason);
        }

        return Result(false, null, false, false, LengthNotSupportedFailureReason);

        ProductIdentifierValidationResult Result(
            bool isValid,
            string? identifierStandard,
            bool checkDigitApplicable,
            bool checkDigitValid,
            string? failureReason) =>
            new(
                identifier,
                normalizedIdentifier,
                isValid,
                identifierStandard,
                checkDigitApplicable,
                checkDigitValid,
                barcodeType,
                normalizedSymbology,
                failureReason);
    }

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
            Unknown => ValidateUnknown(value),
            _ => "Unsupported barcode type.",
        };
    }

    private static string? ValidateUnknown(string value)
    {
        var classification = Classify(value, Unknown);
        if (classification.IsValid)
        {
            return null;
        }

        if (!value.All(char.IsDigit) && Code128Pattern.IsMatch(value))
        {
            return null;
        }

        return classification.FailureReason switch
        {
            ChecksumFailedFailureReason => "Barcode checksum is invalid.",
            NonNumericGtinFailureReason => "Barcode must contain only digits for the selected type.",
            _ => "Unsupported barcode format.",
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
    /// GS1 / GTIN mod-10 checksum (works for GTIN-8, GTIN-12, GTIN-13, and GTIN-14).
    /// </summary>
    public static bool PassesGtinChecksum(string digits)
    {
        if (string.IsNullOrEmpty(digits) || !digits.All(char.IsDigit))
        {
            return false;
        }

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
