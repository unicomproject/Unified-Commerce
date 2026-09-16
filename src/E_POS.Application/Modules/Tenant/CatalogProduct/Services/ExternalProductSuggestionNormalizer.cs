using System.Diagnostics.CodeAnalysis;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Sanitizes untrusted provider payloads into the provider-neutral suggestion model.
/// Rejects identifier mismatches and unusable FOUND payloads.
/// </summary>
public static class ExternalProductSuggestionNormalizer
{
    public const int ShortNameMaxLength = 100;
    public const int BrandTextMaxLength = 200;
    public const int CategoryTextMaxLength = 200;
    public const int UnitTextMaxLength = 100;
    public const int CountryCodeMaxLength = 8;
    public const int ImageCandidateMaxLength = 2048;
    public const int SourceReferenceMaxLength = 200;

    /// <summary>
    /// Returns normalized suggestion or null when FOUND payload is unusable / mismatched.
    /// </summary>
    public static bool TryNormalizeFound(
        ExternalProductLookupRequest request,
        ExternalProductLookupProviderResult providerResult,
        [NotNullWhen(true)] out ExternalProductSuggestion? suggestion,
        out string? sourceReference)
    {
        suggestion = null;
        sourceReference = null;

        if (!string.Equals(providerResult.Status, ExternalProductLookupStatuses.Found, StringComparison.Ordinal))
        {
            return false;
        }

        var raw = providerResult.Suggestion;
        if (raw is null)
        {
            return false;
        }

        var productName = Truncate(Clean(raw.ProductName), ProductConstants.ProductNameMaxLength);
        if (string.IsNullOrEmpty(productName))
        {
            return false;
        }

        var primaryGtin = Clean(raw.PrimaryGtin);
        if (!string.IsNullOrEmpty(primaryGtin) &&
            !string.Equals(primaryGtin, request.Identifier, StringComparison.Ordinal))
        {
            // Do not silently accept a product for a different identifier.
            return false;
        }

        // Prefer requested identifier (preserves leading zeros) when provider omits GTIN.
        primaryGtin = string.IsNullOrEmpty(primaryGtin) ? request.Identifier : primaryGtin;

        var identifierStandard = Clean(raw.IdentifierStandard) ?? Clean(request.IdentifierStandard);
        var imageCandidate = NormalizeImageCandidate(raw.ImageCandidate);

        suggestion = new ExternalProductSuggestion(
            ProductName: productName,
            ShortName: Truncate(Clean(raw.ShortName), ShortNameMaxLength),
            BrandText: Truncate(Clean(raw.BrandText), BrandTextMaxLength),
            CategoryText: Truncate(Clean(raw.CategoryText), CategoryTextMaxLength),
            UnitText: Truncate(Clean(raw.UnitText), UnitTextMaxLength),
            CountryCode: Truncate(Clean(raw.CountryCode), CountryCodeMaxLength),
            ShortDescription: Truncate(Clean(raw.ShortDescription), ProductConstants.ShortDescriptionMaxLength),
            LongDescription: Truncate(Clean(raw.LongDescription), ProductConstants.LongDescriptionMaxLength),
            ImageCandidate: imageCandidate,
            PrimaryGtin: primaryGtin,
            IdentifierStandard: identifierStandard);

        sourceReference = Truncate(Clean(providerResult.ProviderReference), SourceReferenceMaxLength);
        return true;
    }

    private static string? NormalizeImageCandidate(string? value)
    {
        var cleaned = Truncate(Clean(value), ImageCandidateMaxLength);
        if (string.IsNullOrEmpty(cleaned))
        {
            return null;
        }

        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        return cleaned;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
