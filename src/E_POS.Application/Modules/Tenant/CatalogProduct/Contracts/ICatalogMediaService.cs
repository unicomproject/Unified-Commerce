using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICatalogMediaService
{
    Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
        TenantRequestContext context,
        Guid productId,
        ProductImageUploadRequest request,
        MediaUploadFile file,
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
