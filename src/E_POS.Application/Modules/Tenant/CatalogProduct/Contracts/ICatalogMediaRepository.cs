using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICatalogMediaRepository
{
    Task<bool> ProductExistsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<Product?> GetProductForUpdateAsync(
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

    Task<int> CountActiveProductImagesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductImage>> GetActiveProductImagesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<ProductImage?> GetProductImageAsync(
        Guid tenantId,
        Guid productId,
        Guid productImageId,
        CancellationToken cancellationToken);

    Task<MediaAsset?> GetMediaAssetAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    Task<bool> IsMediaAssetLinkedAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantAdminProductImageResponse>> GetProductImageResponsesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);
}
