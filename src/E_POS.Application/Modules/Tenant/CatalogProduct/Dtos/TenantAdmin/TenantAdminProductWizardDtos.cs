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
    
    // Step 4 — Variant Configuration
    public VariantConfigurationDto? VariantConfiguration { get; set; }
    
    // Step 4 — Bundle Configuration
    public BundleConfigurationDto? BundleConfiguration { get; set; }
    
    // Step 5 — Barcode & SKU
    public BarcodeSkuConfigurationDto? BarcodeSkuConfiguration { get; set; }
    
    // Step 6 — Pricing & Tax
    public PricingTaxConfigurationDto? PricingTax { get; set; }
}

public sealed record PricingTaxConfigurationDto(
    decimal? CostPrice,
    decimal? StandardSellingPrice,
    decimal? DiscountPrice,
    Guid? TaxClassId,
    bool? TaxExclusive);

public sealed record PricingTaxResponseDto(
    decimal? CostPrice,
    decimal? StandardSellingPrice,
    decimal? DiscountPrice,
    decimal? EffectiveSellingPrice,
    decimal? DiscountAmount,
    decimal? DiscountPercentage,
    Guid? TaxClassId,
    string? TaxName,
    decimal? TaxRatePercentage,
    bool TaxExclusive = true);

public sealed record BarcodeSkuAssignmentDto(
    Guid? ProductVariantId,
    string? DisplayName,
    string? Sku,
    string? Barcode,
    string? Status,
    string? ClientCombinationKey = null);

public sealed record Step5IdentifierTargetDto(
    Guid? ProductVariantId,
    string DisplayName,
    bool IsAssigned);

public sealed record BarcodeSkuConfigurationDto(
    IReadOnlyList<Step5IdentifierTargetDto>? IdentifierTargets,
    IReadOnlyList<BarcodeSkuAssignmentDto>? Assignments);

public sealed record BundleComponentDto(
    Guid? ComboComponentId,
    Guid ComponentProductId,
    Guid? ComponentVariantId,
    Guid ComponentUomId,
    decimal RequiredQuantity,
    int SortOrder);

public sealed record BundleConfigurationDto(
    Guid? ComboDefinitionId,
    IReadOnlyList<BundleComponentDto> Components);

public sealed record VariantConfigurationOptionValueDto(
    Guid? ProductOptionValueId,
    Guid? SourceOptionTemplateValueId,
    string ValueCode,
    string ValueName,
    string? DisplayName,
    string? ColorHex,
    int SortOrder,
    Guid? ImageMediaAssetId);

public sealed record VariantConfigurationOptionDto(
    Guid? ProductOptionId,
    Guid? SourceOptionTemplateId,
    string OptionCode,
    string OptionName,
    string OptionType,
    string? InputType,
    int SortOrder,
    IReadOnlyList<VariantConfigurationOptionValueDto> Values);

public sealed record VariantConfigurationSelectedValueDto(
    Guid? SourceOptionTemplateId,
    Guid? SourceOptionTemplateValueId,
    string? OptionName,
    string? ValueName);

public sealed record VariantConfigurationVariantDto(
    string ClientCombinationKey,
    Guid? ProductVariantId,
    string? VariantCode,
    string? OptionCombinationHash,
    string? CombinationLabel,
    string? DisplayLabel,
    bool Included,
    string? Status,
    Guid? ExactImageMediaAssetId,
    IReadOnlyList<VariantConfigurationSelectedValueDto> SelectedValues);

public sealed record VariantConfigurationDeletedCombinationDto(
    string ClientCombinationKey,
    Guid? ProductVariantId,
    string? OptionCombinationHash);

public sealed record VariantConfigurationDto(
    IReadOnlyList<VariantConfigurationOptionDto> Options,
    IReadOnlyList<VariantConfigurationVariantDto> Variants,
    IReadOnlyList<VariantConfigurationDeletedCombinationDto> ExcludedCombinationHashes);

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
    IReadOnlyList<ProductUnitConversionResponse>? UnitConversions = null,
    VariantConfigurationDto? VariantConfiguration = null,
    BundleConfigurationDto? BundleConfiguration = null,
    BarcodeSkuConfigurationDto? BarcodeSkuConfiguration = null,
    PricingTaxResponseDto? PricingTax = null,
    int TotalVariantCount = 0,
    int IncludedVariantCount = 0);

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
    IReadOnlyList<ProductUnitConversionResponse>? UnitConversions = null,
    VariantConfigurationDto? VariantConfiguration = null,
    BundleConfigurationDto? BundleConfiguration = null,
    BarcodeSkuConfigurationDto? BarcodeSkuConfiguration = null,
    PricingTaxResponseDto? PricingTax = null,
    int TotalVariantCount = 0,
    int IncludedVariantCount = 0);

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

public sealed record BundleValidationProductProjection(
    Guid ProductId,
    string ProductStructure,
    string Status,
    bool IsSellable,
    bool TrackInventory
);

public sealed record BundleValidationVariantProjection(
    Guid ProductVariantId,
    Guid ProductId,
    string Status,
    bool Included
);

public sealed record BundleValidationUomProjection(
    Guid ComponentProductId,
    Guid? ComponentVariantId,
    Guid UomId,
    bool AllowDecimalQuantity
);
