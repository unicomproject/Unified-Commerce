using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

/// <summary>
/// Public B7 request — Scan Spec §17.2 / API_ENDPOINTS.
/// No tenant duplicate checking; structural validation only before B6.
/// </summary>
public sealed class ExternalLookupProductBarcodeRequest
{
    /// <summary>Required identifier string. Leading zeros preserved.</summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>Optional identifier-standard hint; Classify remains authoritative.</summary>
    public string? IdentifierStandard { get; set; }
}

/// <summary>
/// Public B7 response — status FOUND | NO_MATCH | TEMPORARY_FAILURE.
/// </summary>
/// <param name="SourceReference">Legacy field; retained for backward compatibility. Prefer SourceProvider.</param>
/// <param name="SourceProvider">The real provider identity ("openfoodfacts", etc.), correct on both fresh and cache-hit results. Never "cache".</param>
/// <param name="RetrievalSource">"PROVIDER" or "CACHE" — how this result was served, independent of which provider produced it.</param>
public sealed record ExternalLookupProductBarcodeResponse(
    string Status,
    ExternalProductSuggestion? Suggestion,
    string? SourceReference,
    bool RetryAllowed,
    TenantCategoryResolutionResult? CategoryResolution = null,
    string? SourceProvider = null,
    string? RetrievalSource = null,
    TenantBrandResolutionResult? BrandResolution = null);
