using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductRepository
{
    Task<TenantAdminProductSummaryResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetPrimaryCategoryNamesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetPrimaryImageUrlsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<Guid?> ResolveUnitIdAsync(Guid tenantId, string unitType, CancellationToken cancellationToken);

    Task<bool> CategoryBelongsToTenantAsync(
        Guid tenantId,
        Guid categoryId,
        Guid? parentCategoryId,
        CancellationToken cancellationToken);

    Task<bool> BrandBelongsToTenantAsync(
        Guid tenantId,
        Guid brandId,
        CancellationToken cancellationToken);

    Task<bool> TaxClassBelongsToTenantAsync(
        Guid tenantId,
        Guid taxClassId,
        CancellationToken cancellationToken);

    Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken);

    Task<TenantAdminProductCreateResponse> CreateProductAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminProductCreateRequest request,
        Guid unitId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminProductDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<bool> SkuExistsOnOtherProductAsync(
        Guid tenantId,
        string sku,
        Guid productId,
        CancellationToken cancellationToken);

    Task<bool> BarcodeExistsOnOtherProductAsync(
        Guid tenantId,
        string barcode,
        Guid productId,
        CancellationToken cancellationToken);

    Task<TenantAdminProductDetailResponse?> UpdateProductAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        TenantAdminProductCreateRequest request,
        Guid unitId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminProductStatusUpdateResponse?> UpdateProductStatusAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string status,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminProductActivationSnapshot?> GetActivationSnapshotAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<TenantAdminProductDeleteHistoryFlags?> GetDeleteHistoryFlagsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<TenantAdminProductDeleteOperationResult> DeleteProductAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminProductDashboardRawData> GetDashboardAsync(
        Guid tenantId,
        TenantAdminProductDashboardQuery query,
        CancellationToken cancellationToken);
}
