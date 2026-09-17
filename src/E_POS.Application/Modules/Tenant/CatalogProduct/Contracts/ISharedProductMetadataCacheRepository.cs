using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ISharedProductMetadataCacheRepository
{
    Task<ExternalProductSuggestion?> GetValidAsync(
        string normalizedBarcode,
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
