using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

/// <summary>
/// Scanner-first draft create bootstrap (B8). Orchestration input for
/// <c>POST .../products/draft</c> only — not a DB column owner for creationAction.
/// </summary>
public sealed class ProductSetupScanBootstrapRequest
{
    public string? AcquisitionMode { get; set; }
    public string? CreationAction { get; set; }
    public string? CandidateIdentifier { get; set; }
    public string? IdentifierStandard { get; set; }
    public string? SymbologyHint { get; set; }
    public string? NoBarcodeReason { get; set; }
    public string? ExternalLookupStatus { get; set; }
    public string? ExternalSourceReference { get; set; }
    public ExternalProductSuggestion? NormalizedPrefill { get; set; }
    public string? GeneratedSkuCandidate { get; set; }
}

/// <summary>
/// Normalized scan-context persistence payload produced by B8 validation (server-owned fields).
/// </summary>
public sealed record ProductSetupScanBootstrapPersistence(
    string AcquisitionMode,
    string CreationAction,
    string? CandidateIdentifier,
    string? IdentifierStandard,
    string? SymbologyHint,
    string? NoBarcodeReason,
    string? ExternalLookupStatus,
    string? ExternalSourceReference,
    string? NormalizedPrefillJson,
    string? GeneratedSkuCandidate,
    bool ApplyPrefillToDraft);
