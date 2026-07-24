using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICatalogMediaRepository
{
    Task<bool> ProductExistsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<bool> ProductVariantExistsAsync(
        Guid tenantId,
        Guid productId,
        Guid productVariantId,
        CancellationToken cancellationToken);

    Task<Category?> GetCategoryForImageUpdateAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<Brand?> GetBrandForLogoUpdateAsync(
        Guid tenantId,
        Guid brandId,
        CancellationToken cancellationToken);

    Task AddMediaAssetAsync(
        MediaAsset mediaAsset,
        CancellationToken cancellationToken);

    Task AddProductImageAsync(
        ProductImage productImage,
        CancellationToken cancellationToken);

    Task MarkMediaAssetInactiveAsync(
        Guid tenantId,
        Guid mediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
