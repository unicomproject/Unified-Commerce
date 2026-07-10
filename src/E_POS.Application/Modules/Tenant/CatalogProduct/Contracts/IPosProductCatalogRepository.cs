using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IPosProductCatalogRepository
{
    Task<PosProductCatalogRepositoryResult> ListProductsAsync(
        Guid tenantId,
        Guid deviceId,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken);

    Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken);
}

public sealed record PosProductCatalogRepositoryResult(
    string? ErrorCode,
    IReadOnlyList<PosProductSummaryResponseDto> Products)
{
    public bool IsSuccess => ErrorCode is null;
}

public sealed record PosProductCatalogCategoriesRepositoryResult(
    string? ErrorCode,
    IReadOnlyList<PosCatalogCategoryResponseDto> Categories)
{
    public bool IsSuccess => ErrorCode is null;
}
