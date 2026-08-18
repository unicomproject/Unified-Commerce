namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

/// <summary>
/// Final Step 7 Create Product payload for the 7-step Add Product wizard.
/// Independent of POST/PUT draft. Creates the complete Product graph atomically.
/// </summary>
public sealed class TenantAdminWizardProductCreateRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }

    public bool DesiredPublishActive { get; set; } = true;
    public bool PosSellable { get; set; } = true;
    public bool AllowOnlineSale { get; set; } = true;
    public bool TrackInventory { get; set; } = true;
    public bool BatchTracking { get; set; }
    public bool ExpiryTracking { get; set; }
    public bool SerialTracking { get; set; }

    /// <summary>SIMPLE | VARIANT (BUNDLE deferred).</summary>
    public string ProductStructure { get; set; } = "SIMPLE";

    // SIMPLE — Units & Pack Conversion
    public string? UnitModel { get; set; }
    public Guid? ProductUnitId { get; set; }
    public Guid? BaseUnitId { get; set; }
    public Guid? SellingUnitId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public Guid? OuterPackUnitId { get; set; }
    public decimal? ItemsPerPurchaseUnit { get; set; }
    public decimal? PurchaseUnitsPerOuterPack { get; set; }
    public bool AllowDecimalQuantity { get; set; }

    // VARIANT
    public VariantConfigurationDto? VariantConfiguration { get; set; }

    // Identifiers (SIMPLE uses product-level / default variant; VARIANT uses ClientCombinationKey)
    public BarcodeSkuConfigurationDto? BarcodeSkuConfiguration { get; set; }

    // Pricing & Tax
    public PricingTaxConfigurationDto? PricingTax { get; set; }

    public IReadOnlyList<Guid>? StagedMediaAssetIds { get; set; }

    /// <summary>Optional client idempotency key to prevent double-create.</summary>
    public string? IdempotencyKey { get; set; }
}
