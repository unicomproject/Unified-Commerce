using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IBrandRepository
{
    Task<bool> BrandCodeExistsAsync(Guid tenantId, string brandCode, Guid? excludeBrandId, CancellationToken cancellationToken);
    Task<BrandListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<BrandResponse?> GetByIdAsync(Guid tenantId, Guid brandId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Brand?> GetEditableAsync(Guid tenantId, Guid brandId, CancellationToken cancellationToken);
    Task AddAsync(Brand brand, CancellationToken cancellationToken);
    Task AddMediaAssetAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
    Task MarkMediaAssetInactiveAsync(Guid tenantId, Guid mediaAssetId, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

