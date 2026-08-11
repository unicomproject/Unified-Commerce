using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICatalogMediaService
{
    Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
        TenantRequestContext context,
        Guid productId,
        ProductImageUploadRequest request,
        MediaUploadFile file,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StagedProductImageResponse>> StageProductImageAsync(
        TenantRequestContext context,
        MediaUploadFile file,
        Guid? uploadSessionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductImagesMutationResponse>> ReorderProductImagesAsync(
        TenantRequestContext context,
        Guid productId,
        ReorderProductImagesRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductImagesMutationResponse>> DeleteProductImageAsync(
        TenantRequestContext context,
        Guid productId,
        Guid productImageId,
        long? expectedRowVersion,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductImagesMutationResponse>> ReplaceProductImagesAsync(
        TenantRequestContext context,
        Guid productId,
        long expectedRowVersion,
        IReadOnlyList<MediaUploadFile>? files,
        IReadOnlyList<Guid>? stagedMediaAssetIds,
        CancellationToken cancellationToken);

    Task<ApplicationResult<MediaAssetUploadResponse>> UploadCategoryImageAsync(
        TenantRequestContext context,
        Guid categoryId,
        MediaUploadFile file,
        CancellationToken cancellationToken);

    Task<ApplicationResult<MediaAssetUploadResponse>> UploadBrandLogoAsync(
        TenantRequestContext context,
        Guid brandId,
        MediaUploadFile file,
        CancellationToken cancellationToken);
}
