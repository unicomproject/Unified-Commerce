namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed class ResolveProductBarcodeRequest
{
    public string Barcode { get; set; } = string.Empty;

    /// <summary>SCAN | MANUAL — telemetry only; does not change lookup semantics.</summary>
    public string InputMode { get; set; } = "SCAN";

    /// <summary>Optional physical symbology. Never a GTIN value.</summary>
    public string? ReportedSymbology { get; set; }
}

public sealed record ResolveProductBarcodeResponse(
    string Outcome,
    string? NormalizedBarcode,
    string? IdentifierStandard,
    string? BarcodeType,
    string? InvalidReason,
    ResolveProductBarcodeLocalMatchDto? LocalMatch);

public sealed record ResolveProductBarcodeLocalMatchDto(
    string MatchedAt,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantLabel,
    string? Brand,
    string? Category,
    string? Sku,
    decimal? SellingPrice,
    string? Currency,
    string Status,
    string? ImageUrl,
    bool CanViewProduct,
    bool CanEditProduct);

public sealed record ProductBarcodeResolveMatchProjection(
    Guid ProductId,
    Guid? VariantId,
    string MatchedAt,
    string ProductName,
    string? VariantLabel,
    string? BrandName,
    string? CategoryName,
    string? Sku,
    string ProductStatus,
    string? ImageUrl,
    int MatchCount);
