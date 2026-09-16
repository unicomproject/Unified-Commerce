namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

public static class ExternalProductLookupStatuses
{
    public const string Found = "FOUND";
    public const string NoMatch = "NO_MATCH";
    public const string TemporaryFailure = "TEMPORARY_FAILURE";
}

/// <summary>
/// Already-validated identifier context for provider/coordinator (B7 will gate validation).
/// Identifier remains a string — leading zeros preserved.
/// </summary>
public sealed record ExternalProductLookupRequest(
    string Identifier,
    string? IdentifierStandard,
    string? BarcodeType);

/// <summary>
/// Provider-neutral suggestion fields (API_ENDPOINTS / Scan Spec). Suggestion text only — never tenant master IDs.
/// </summary>
public sealed record ExternalProductSuggestion(
    string? ProductName,
    string? ShortName,
    string? BrandText,
    string? CategoryText,
    string? UnitText,
    string? CountryCode,
    string? ShortDescription,
    string? LongDescription,
    string? ImageCandidate,
    string? PrimaryGtin,
    string? IdentifierStandard);

public sealed record ExternalProductLookupProviderResult(
    string Status,
    ExternalProductSuggestion? Suggestion,
    string? ProviderReference,
    string? FailureCategory);

/// <summary>
/// Coordinator outcome for future B7 public contract mapping.
/// </summary>
public sealed record ExternalProductLookupResult(
    string Status,
    ExternalProductSuggestion? Suggestion,
    string? SourceReference,
    bool RetryAllowed);
