using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

/// <summary>
/// Repository contract for tenant-specific external category mappings.
/// </summary>
public interface IExternalCategoryMappingRepository
{
    Task<ExternalCategoryMapping?> GetAsync(
        Guid tenantId,
        string provider,
        string externalCategoryKey,
        CancellationToken cancellationToken);

    Task<ExternalCategoryMapping> UpsertAsync(
        Guid tenantId,
        string provider,
        string externalCategoryKey,
        string externalCategoryName,
        Guid tenantCategoryId,
        string mappingSource,
        Guid? userId,
        CancellationToken cancellationToken,
        bool saveChanges = true);
}
