using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

/// <summary>
/// Provider-neutral external product-data adapter. Implementations live in Infrastructure when approved.
/// Must not expose credentials, raw vendor payloads, or mutate catalogue/master data.
/// </summary>
public interface IExternalProductLookupProvider
{
    /// <summary>Logical name matched to <c>ExternalProductLookupOptions.Providers[].Name</c>.</summary>
    string Name { get; }

    bool CanHandle(ExternalProductLookupRequest request);

    Task<ExternalProductLookupProviderResult> LookupAsync(
        ExternalProductLookupRequest request,
        CancellationToken cancellationToken);
}
