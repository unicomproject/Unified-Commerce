using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IPosProductCatalogService
{
    Task<ApplicationResult<IReadOnlyList<PosProductSummaryResponseDto>>> ListProductsAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<PosCatalogCategoryResponseDto>>> ListCategoriesAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosProductDetailResponseDto>> GetProductDetailAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosBarcodeProductResponseDto>> GetProductByBarcodeAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? barcode,
        CancellationToken cancellationToken);
}
