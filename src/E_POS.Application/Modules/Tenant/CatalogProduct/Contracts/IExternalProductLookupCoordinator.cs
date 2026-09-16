using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

/// <summary>
/// Coordinates zero/one/many configured external product-data providers.
/// Side-effect free. Does not expose HTTP endpoints (B7 owns public API).
/// </summary>
public interface IExternalProductLookupCoordinator
{
    Task<ExternalProductLookupResult> LookupAsync(
        ExternalProductLookupRequest request,
        CancellationToken cancellationToken);
}
