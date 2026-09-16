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
public sealed record ExternalLookupProductBarcodeResponse(
    string Status,
    ExternalProductSuggestion? Suggestion,
    string? SourceReference,
    bool RetryAllowed);
