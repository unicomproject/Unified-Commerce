namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed record TenantCategoryResolutionRequest(
    Guid TenantId,
    string Provider,
    string? ExternalCategoryKey,
    string? ExternalCategoryName,
    IReadOnlyList<string>? ExternalCategoryHierarchy = null);

public sealed record TenantCategoryCandidate(
    Guid Id,
    string Name,
    string Code);

public sealed record TenantCategorySuggestionItem(
    Guid Id,
    string Name,
    string Code,
    string MatchType);

public sealed record TenantCategoryResolutionResult(
    string Provider,
    string? ExternalCategoryKey,
    string? ExternalCategoryName,
    TenantCategoryCandidate? MappedCategory,
    IReadOnlyList<TenantCategorySuggestionItem> Suggestions);

/// <summary>
/// Optional external category mapping context provided during product creation.
/// </summary>
public sealed class ExternalCategoryMappingContext
{
    public ExternalCategoryMappingContext() { }

    public ExternalCategoryMappingContext(string provider, string externalCategoryKey, string? externalCategoryName = null)
    {
        Provider = provider;
        ExternalCategoryKey = externalCategoryKey;
        ExternalCategoryName = externalCategoryName;
    }

    public string Provider { get; set; } = string.Empty;
    public string ExternalCategoryKey { get; set; } = string.Empty;
    public string? ExternalCategoryName { get; set; }
}
