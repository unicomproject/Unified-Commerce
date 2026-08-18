using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductRepository
{
    Task<TenantAdminProductSummaryResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<TenantAdminProductListResponse> GetPagedListAsync(
        Guid tenantId,
        string? search,
        Guid? categoryId,
        Guid? brandId,
        string? productStatus,
        string? stockStatus,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        bool canViewStock,
        CancellationToken cancellationToken);

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

    Task<TenantAdminProductFilterOptionsResponse> GetFilterOptionsAsync(
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
        Guid? userId,
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

    Task<bool> ActiveCategoryExistsAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<bool> ProductCodeExistsAsync(Guid tenantId, string productCode, Guid? excludeProductId, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductVariantId, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(Guid tenantId, string barcodeValue, Guid? excludeProductVariantId, CancellationToken cancellationToken);
    Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<Guid?> GetDefaultInventoryUomIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<string?> GetTenantStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<bool> IsInitialCreationDraftAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<SaveProductDraftResult> SaveProductDraftAsync(
        Guid tenantId,
        Guid userId,
        SaveProductDraftCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<ProductSetupWizardDto?> GetSetupAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<bool> HasOperationalHistoryAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BundleValidationProductProjection>> GetProductsForBundleValidationAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BundleValidationVariantProjection>> GetVariantsForBundleValidationAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BundleValidationUomProjection>> GetComponentUomValidationDataAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> componentProductIds,
        IReadOnlyCollection<Guid> componentVariantIds,
        IReadOnlyCollection<Guid> componentUomIds,
        CancellationToken cancellationToken);

    Task SaveVariantsAsync(
        Guid tenantId,
        Guid productId,
        VariantConfigurationDto variantConfiguration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomic final Create from the 7-step wizard (no draft pipeline).
    /// </summary>
    Task<SaveProductDraftResult> CreateProductFromWizardAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminWizardProductCreateRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
