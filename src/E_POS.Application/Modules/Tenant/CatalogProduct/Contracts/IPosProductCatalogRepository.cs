using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IPosProductCatalogRepository
{
    Task<PosProductCatalogRepositoryResult> ListProductsAsync(
        Guid tenantId,
        Guid deviceId,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken,
        Guid? outletId = null,
        string? segment = null);

    Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken);

    Task<PosProductDetailRepositoryResult> GetProductDetailAsync(
        Guid tenantId,
        Guid deviceId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<PosBarcodeProductRepositoryResult> GetProductByBarcodeAsync(
        Guid tenantId,
        Guid deviceId,
        string barcode,
        CancellationToken cancellationToken);

    Task<PosProductRecommendationsRepositoryResult> GetRecommendationsAsync(
        Guid tenantId, Guid deviceId, Guid productId, Guid? sourceVariantId,
        string recommendationType, int limit, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosProductRecommendationsRepositoryResult(null, []));
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

public sealed record PosProductDetailRepositoryResult(
    string? ErrorCode,
    PosProductDetailResponseDto? Product)
{
    public bool IsSuccess => ErrorCode is null;
}

public sealed record PosBarcodeProductRepositoryResult(
    string? ErrorCode,
    PosBarcodeProductResponseDto? Product)
{
    public bool IsSuccess => ErrorCode is null;
}

public sealed record PosProductRecommendationsRepositoryResult(
    string? ErrorCode,
    IReadOnlyList<PosProductRecommendationResponseDto> Recommendations)
{
    public bool IsSuccess => ErrorCode is null;
}
