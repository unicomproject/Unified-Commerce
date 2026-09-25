using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ISharedProductMetadataCacheRepository
{
    /// <summary>
    /// Looks up a valid (unexpired) cache entry scoped to BOTH the barcode AND the requesting
    /// provider. Must never return an entry cached under a different provider — that would let
    /// one provider's cached data masquerade as another's.
    /// </summary>
    Task<CachedExternalProductLookupResult?> GetValidAsync(
        string normalizedBarcode,
        string provider,
        CancellationToken cancellationToken);

    Task SetAsync(
        string normalizedBarcode,
        string? identifierStandard,
        string provider,
        ExternalProductSuggestion suggestion,
        string? rawResponseJson,
        TimeSpan ttl,
        CancellationToken cancellationToken);
}
