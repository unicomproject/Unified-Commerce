namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

public static class ExternalProductLookupStatuses
{
    public const string Found = "FOUND";
    public const string NoMatch = "NO_MATCH";
    public const string TemporaryFailure = "TEMPORARY_FAILURE";
}

/// <summary>
/// Distinguishes HOW a FOUND result was served, independent of WHICH provider produced it
/// (<see cref="ExternalProductLookupResult.SourceProvider"/>). Never conflate the two — a cache
/// hit for OpenFoodFacts must still report SourceProvider="openfoodfacts", RetrievalSource="CACHE".
/// </summary>
public static class ExternalProductLookupRetrievalSources
{
    public const string Provider = "PROVIDER";
    public const string Cache = "CACHE";
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
    string? IdentifierStandard,
    string? ExternalCategoryKey = null,
    string? ExternalCategoryName = null,
    IReadOnlyList<string>? ExternalCategoryHierarchy = null);

public sealed record ExternalProductLookupProviderResult(
    string Status,
    ExternalProductSuggestion? Suggestion,
    string? ProviderReference,
    string? FailureCategory);

/// <summary>
/// Result returned by the shared metadata cache when a valid (unexpired) entry exists for the
/// requested provider. Carries the entry's real provider identity so callers never have to infer
/// it — the cache is a retrieval mechanism, not a provider, and must never be reported as one.
/// </summary>
public sealed record CachedExternalProductLookupResult(
    ExternalProductSuggestion Suggestion,
    string Provider);

/// <summary>
/// Coordinator outcome for future B7 public contract mapping.
/// </summary>
/// <param name="SourceReference">
/// Legacy provider-specific reference (historically doubled as a de-facto provider identity on
/// fresh lookups only). Retained for backward compatibility; prefer <see cref="SourceProvider"/>.
/// </param>
/// <param name="SourceProvider">
/// The real, stable identity of the provider that produced this data ("openfoodfacts", etc.),
/// whether served fresh or from cache. Never the literal "cache".
/// </param>
/// <param name="RetrievalSource">
/// How this result was served: <see cref="ExternalProductLookupRetrievalSources.Provider"/> or
/// <see cref="ExternalProductLookupRetrievalSources.Cache"/>. Orthogonal to SourceProvider.
/// </param>
public sealed record ExternalProductLookupResult(
    string Status,
    ExternalProductSuggestion? Suggestion,
    string? SourceReference,
    bool RetryAllowed,
    string? SourceProvider = null,
    string? RetrievalSource = null);
