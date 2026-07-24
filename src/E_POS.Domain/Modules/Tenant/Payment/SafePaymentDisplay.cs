using System.Text.Json;
using System.Text.RegularExpressions;

namespace E_POS.Domain.Modules.Tenant.Payment.Entities;

/// <summary>
/// PCI-safe payment display helpers. Never accepts or returns full PAN, CVV, PIN, or secrets.
/// </summary>
public static class SafePaymentDisplay
{
    private static readonly Regex Last4Pattern = new("^[0-9]{4}$", RegexOptions.Compiled);
    private static readonly Regex BrandPattern = new("^[A-Za-z0-9][A-Za-z0-9 _\\-]{0,29}$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string? NormalizeLast4(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Exactly four numeric digits only — never derive last4 from a longer PAN or token.
        var trimmed = value.Trim();
        return Last4Pattern.IsMatch(trimmed) ? trimmed : null;
    }

    public static string? NormalizeBrand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!BrandPattern.IsMatch(trimmed))
        {
            return null;
        }

        return trimmed.ToUpperInvariant() switch
        {
            "VISA" => "Visa",
            "MASTERCARD" or "MASTER CARD" or "MC" => "Mastercard",
            "AMEX" or "AMERICAN EXPRESS" => "Amex",
            "DISCOVER" => "Discover",
            _ => char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant()
        };
    }

    public static string? ToSanitizedCardMetadataJson(string? cardBrand, string? cardLast4)
    {
        var brand = NormalizeBrand(cardBrand);
        var last4 = NormalizeLast4(cardLast4);
        if (brand is null && last4 is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            new SanitizedCardMetadata(brand, last4),
            JsonOptions);
    }

    public static (string? Brand, string? Last4) TryParseSanitizedCardMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, null);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<SanitizedCardMetadata>(json, JsonOptions);
            if (parsed is null)
            {
                return (null, null);
            }

            return (NormalizeBrand(parsed.CardBrand), NormalizeLast4(parsed.CardLast4));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Only a verified 4-digit tip is maskable. Long provider tokens are never truncated for display.
    /// </summary>
    public static string? ResolveLast4(string? sanitizedJson, string? paymentExternalReference)
    {
        var (_, last4FromJson) = TryParseSanitizedCardMetadata(sanitizedJson);
        if (last4FromJson is not null)
        {
            return last4FromJson;
        }

        // Allow ExternalReference to hold last4 only when it is exactly four digits.
        return NormalizeLast4(
            !string.IsNullOrWhiteSpace(paymentExternalReference) &&
            Last4Pattern.IsMatch(paymentExternalReference.Trim())
                ? paymentExternalReference
                : null);
    }

    public static string? FormatMaskedReference(string? cardLast4)
    {
        var last4 = NormalizeLast4(cardLast4);
        return last4 is null ? null : $"•••• {last4}";
    }

    public static string ResolveMethodLabel(
        string? methodName,
        string? methodCode,
        string? cardBrand)
    {
        var brand = NormalizeBrand(cardBrand);
        if (brand is not null)
        {
            return brand;
        }

        if (!string.IsNullOrWhiteSpace(methodName))
        {
            return methodName.Trim();
        }

        return string.IsNullOrWhiteSpace(methodCode) ? string.Empty : methodCode.Trim();
    }

    private sealed record SanitizedCardMetadata(
        string? CardBrand,
        string? CardLast4);
}
