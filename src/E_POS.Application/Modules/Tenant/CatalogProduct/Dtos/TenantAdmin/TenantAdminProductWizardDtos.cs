namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed class SaveProductDraftRequest
{
    public string? ProductName { get; set; }
    public string? ShortName { get; set; }
    public string? ProductCode { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public bool DesiredPublishActive { get; set; } = true;
    public bool PosSellable { get; set; } = true;
    public bool TrackInventory { get; set; } = true;
    public bool BatchTracking { get; set; }
    public bool ExpiryTracking { get; set; }
    public bool SerialTracking { get; set; }
    public string? ProductStructure { get; set; }
    public bool AllowOnlineSale { get; set; }
    public int CurrentSetupStep { get; set; } = 1;
    public bool AdvanceStep { get; set; }
    public string? WizardAction { get; set; }
    public long? ExpectedRowVersion { get; set; }
    public IReadOnlyList<Guid>? StagedMediaAssetIds { get; set; }

    // Step 3 — Units & Pack Conversion Properties
    public string? UnitModel { get; set; }
    public Guid? ProductUnitId { get; set; }
    public Guid? BaseUnitId { get; set; }
    public Guid? SellingUnitId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public Guid? OuterPackUnitId { get; set; }
    public decimal? ItemsPerPurchaseUnit { get; set; }
    public decimal? PurchaseUnitsPerOuterPack { get; set; }
    public bool AllowDecimalQuantity { get; set; }
}

public sealed record ProductUnitConversionResponse(
    Guid UomId,
    string UomCode,
    string UomName,
    string UnitLevel,
    decimal ConversionToBaseFactor,
    bool IsBaseUnit,
    bool IsSellingUnit,
    bool IsPurchaseUnit,
    bool IsOuterPackUnit);

public sealed record ProductDraftResponse(
    Guid ProductId,
    string ProductName,
    string? ProductCode,
    string Status,
    string? DesiredPublishStatus,
    int CurrentSetupStep,
    DateTimeOffset? DraftSavedAt,
    long RowVersion,
    Guid? CategoryId,
    Guid? BrandId,
    string? ShortDescription,
    string? LongDescription,
    bool PosSellable,
    bool TrackInventory,
    bool BatchTracking,
    bool ExpiryTracking,
    bool SerialTracking,
    string ProductStructure,
    bool AllowOnlineSale,
    IReadOnlyList<TenantAdminProductImageResponse> Images,
    string? CategoryName = null,
    string? BrandName = null,
    Guid? CreatedByTenantUserId = null,
    string? CreatedByName = null,
    DateTimeOffset? CreatedAt = null,
    string? Sku = null,
    string? PrimaryImageUrl = null,
    string? InventoryMethod = null,
    int ComponentCount = 0,
    bool ComponentsConfigured = false,
    int? TargetSetupStep = null,
    int? LastCompletedSetupStep = null,
    string? UnitModel = null,
    Guid? BaseUnitId = null,
    string? BaseUnitName = null,
    Guid? SellingUnitId = null,
    string? SellingUnitName = null,
    Guid? PurchaseUnitId = null,
    string? PurchaseUnitName = null,
    Guid? OuterPackUnitId = null,
    string? OuterPackUnitName = null,
    decimal? ItemsPerPurchaseUnit = null,
    decimal? PurchaseUnitsPerOuterPack = null,
    bool AllowDecimalQuantity = false,
    IReadOnlyList<ProductUnitConversionResponse>? UnitConversions = null);

public sealed record ProductSetupWizardDto(
    Guid ProductId,
    string ProductName,
    string? ProductCode,
    string Status,
    string? DesiredPublishStatus,
    int CurrentSetupStep,
    DateTimeOffset? DraftSavedAt,
    long RowVersion,
    Guid? CategoryId,
    Guid? BrandId,
    string? ShortDescription,
    string? LongDescription,
    bool PosSellable,
    bool TrackInventory,
    bool BatchTracking,
    bool ExpiryTracking,
    bool SerialTracking,
    string ProductStructure,
    bool AllowOnlineSale,
    IReadOnlyList<TenantAdminProductImageResponse> Images,
    string? CategoryName = null,
    string? BrandName = null,
    Guid? CreatedByTenantUserId = null,
    string? CreatedByName = null,
    DateTimeOffset? CreatedAt = null,
    string? Sku = null,
    string? PrimaryImageUrl = null,
    string? InventoryMethod = null,
    int ComponentCount = 0,
    bool ComponentsConfigured = false,
    int? TargetSetupStep = null,
    int? LastCompletedSetupStep = null,
    string? UnitModel = null,
    Guid? BaseUnitId = null,
    string? BaseUnitName = null,
    Guid? SellingUnitId = null,
    string? SellingUnitName = null,
    Guid? PurchaseUnitId = null,
    string? PurchaseUnitName = null,
    Guid? OuterPackUnitId = null,
    string? OuterPackUnitName = null,
    decimal? ItemsPerPurchaseUnit = null,
    decimal? PurchaseUnitsPerOuterPack = null,
    bool AllowDecimalQuantity = false,
    IReadOnlyList<ProductUnitConversionResponse>? UnitConversions = null);

public sealed record TenantAdminProductSalesChannelOptionResponse(
    Guid SalesChannelId,
    string ChannelCode,
    string ChannelName,
    string ChannelType);

public sealed record StagedProductImageResponse(
    Guid MediaAssetId,
    string? PublicUrl,
    string FileName,
    string MimeType,
    long FileSizeBytes,
    DateTimeOffset CreatedAt,
    string Status = "STAGED");

public sealed record ReorderProductImagesRequest(
    long ExpectedRowVersion,
    Guid? PrimaryProductImageId,
    IReadOnlyList<ReorderProductImageItem> Items);

public sealed record ReorderProductImageItem(Guid ProductImageId, int SortOrder);

public sealed record ProductImagesMutationResponse(
    Guid ProductId,
    long RowVersion,
    IReadOnlyList<TenantAdminProductImageResponse> Images);

public sealed record ReplaceProductImagesRequest(
    long ExpectedRowVersion,
    IReadOnlyList<Guid>? StagedMediaAssetIds);
