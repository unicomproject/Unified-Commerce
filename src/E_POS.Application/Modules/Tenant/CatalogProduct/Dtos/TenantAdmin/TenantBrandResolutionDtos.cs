namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed record TenantBrandResolutionRequest(
    Guid TenantId,
    string Provider,
    string? ExternalBrandKey,
    string? ExternalBrandName);

public sealed record TenantBrandCandidate(
    Guid Id,
    string Name,
    string Code);

public sealed record TenantBrandSuggestionItem(
    Guid Id,
    string Name,
    string Code,
    string MatchType);

public sealed record TenantBrandResolutionResult(
    string Provider,
    string? ExternalBrandKey,
    string? ExternalBrandName,
    TenantBrandCandidate? MappedBrand,
    IReadOnlyList<TenantBrandSuggestionItem> Suggestions);

/// <summary>
/// Optional external brand mapping context provided during product creation.
/// </summary>
public sealed class ExternalBrandMappingContext
{
    public ExternalBrandMappingContext() { }

    public ExternalBrandMappingContext(string provider, string externalBrandKey, string? externalBrandName = null)
    {
        Provider = provider;
        ExternalBrandKey = externalBrandKey;
        ExternalBrandName = externalBrandName;
    }

    public string Provider { get; set; } = string.Empty;
    public string ExternalBrandKey { get; set; } = string.Empty;
    public string? ExternalBrandName { get; set; }
}
