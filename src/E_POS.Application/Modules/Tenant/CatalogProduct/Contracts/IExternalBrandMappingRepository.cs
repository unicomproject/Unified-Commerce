using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

/// <summary>
/// Repository contract for tenant-specific external brand mappings.
/// </summary>
public interface IExternalBrandMappingRepository
{
    Task<ExternalBrandMapping?> GetAsync(
        Guid tenantId,
        string provider,
        string externalBrandKey,
        CancellationToken cancellationToken);

    Task<ExternalBrandMapping> UpsertAsync(
        Guid tenantId,
        string provider,
        string externalBrandKey,
        string externalBrandName,
        Guid tenantBrandId,
        string mappingSource,
        Guid? userId,
        CancellationToken cancellationToken,
        bool saveChanges = true);
}
