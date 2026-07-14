namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed record TenantAdminProductSummaryResponse(
    int TotalProducts,
    int ActiveProducts,
    int InactiveProducts,
    int ProductCategories);

public sealed record TenantAdminProductSummaryCardsResponse(
    int TotalProducts,
    int ActiveProducts,
    int InactiveProducts,
    int CategoryCount);

public sealed record TenantAdminProductCategoryOptionResponse(
    Guid CategoryId,
    string CategoryName,
    string CategoryCode);

public sealed record TenantAdminProductSubCategoryOptionResponse(
    Guid SubCategoryId,
    string SubCategoryName,
    string SubCategoryCode,
    Guid ParentCategoryId);

public sealed record TenantAdminProductBrandOptionResponse(
    Guid BrandId,
    string BrandName,
    string BrandCode);

public sealed record TenantAdminProductUnitOptionResponse(
    Guid UnitId,
    string UnitCode,
    string UnitName);

public sealed record TenantAdminProductTaxOptionResponse(
    Guid TaxId,
    string TaxCode,
    string TaxName);

public sealed record TenantAdminProductOutletOptionResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode);

public sealed record TenantAdminProductVariantOptionTemplateResponse(
    Guid TemplateId,
    string TemplateCode,
    string TemplateName,
    string OptionType);

public sealed record TenantAdminProductCreateOptionsResponse(
    IReadOnlyList<TenantAdminProductCategoryOptionResponse> Categories,
    IReadOnlyList<TenantAdminProductSubCategoryOptionResponse> SubCategories,
    IReadOnlyList<TenantAdminProductBrandOptionResponse> Brands,
    IReadOnlyList<TenantAdminProductUnitOptionResponse> Units,
    IReadOnlyList<TenantAdminProductTaxOptionResponse> Taxes,
    IReadOnlyList<TenantAdminProductOutletOptionResponse> Outlets,
    IReadOnlyList<TenantAdminProductVariantOptionTemplateResponse> VariantOptionTemplates);

public sealed class TenantAdminProductVariantCreateRequest
{
    public string? VariantName { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? Status { get; set; }
}

public sealed class TenantAdminProductStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed record TenantAdminProductStatusUpdateResponse(
    Guid ProductId,
    string Status);

public sealed record TenantAdminProductActivationSnapshot(
    string ProductName,
    string? Sku,
    bool HasCategory,
    decimal SellingPrice,
    string UnitType);

public sealed record TenantAdminProductDeleteHistoryFlags(
    bool HasSales,
    bool HasStockMovements,
    bool HasBatches,
    bool HasReturns,
    bool HasAuditHistory);

public sealed record TenantAdminProductDeleteResponse(
    Guid ProductId,
    string Outcome,
    string Status);

public sealed class TenantAdminProductCreateRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? SubCategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string UnitType { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public Guid? TaxId { get; set; }
    public bool TrackInventory { get; set; }
    public decimal? OpeningStockQuantity { get; set; }
    public decimal? MinimumStockAlertQuantity { get; set; }
    public decimal? MaximumStockQuantity { get; set; }
    public string? StockUnit { get; set; }
    public IReadOnlyList<Guid>? OutletIds { get; set; }
    public bool HasVariants { get; set; }
    public IReadOnlyList<TenantAdminProductVariantCreateRequest>? Variants { get; set; }
    public bool HasExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public int? ExpiryAlertDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool SaveAsDraft { get; set; }
}

public sealed record TenantAdminProductCreateResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    string Status);

public sealed record TenantAdminProductStockDetailResponse(
    decimal? OpeningStockQuantity,
    decimal? MinimumStockAlertQuantity,
    decimal? MaximumStockQuantity,
    string? StockUnit,
    decimal OnHandQuantity,
    decimal AvailableQuantity);

public sealed record TenantAdminProductOutletDetailResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode,
    decimal OnHandQuantity,
    decimal AvailableQuantity);

public sealed record TenantAdminProductVariantDetailResponse(
    Guid VariantId,
    string? VariantName,
    string Sku,
    string? Barcode,
    decimal SellingPrice,
    decimal? DiscountPrice,
    string Status);

public sealed record TenantAdminProductBatchDetailResponse(
    string BatchNumber,
    DateOnly? ManufactureDate,
    DateOnly? ExpiryDate,
    int? ExpiryAlertDays);

public sealed record TenantAdminProductDetailResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    string? Barcode,
    Guid CategoryId,
    string CategoryName,
    Guid? SubCategoryId,
    Guid? BrandId,
    string UnitType,
    string? ShortDescription,
    string? ImageUrl,
    decimal? CostPrice,
    decimal SellingPrice,
    decimal? DiscountPrice,
    Guid? TaxId,
    string? TaxName,
    string Status,
    bool TrackInventory,
    TenantAdminProductStockDetailResponse? Stock,
    IReadOnlyList<TenantAdminProductOutletDetailResponse> Outlets,
    IReadOnlyList<TenantAdminProductVariantDetailResponse> Variants,
    TenantAdminProductBatchDetailResponse? BatchDetails,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantAdminProductListItemResponse(
    Guid Id,
    string Name,
    string? CategoryName,
    string Sku,
    string? Barcode,
    decimal? SellingPrice,
    string Status,
    int OutletCount);

public sealed record TenantAdminProductListResponse(
    TenantAdminProductSummaryResponse Summary,
    IReadOnlyList<TenantAdminProductListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
