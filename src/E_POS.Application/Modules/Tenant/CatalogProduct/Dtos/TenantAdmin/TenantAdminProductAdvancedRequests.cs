namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed class TenantAdminProductVariantUpdateRequest
{
    public string? Sku { get; set; }
    public bool IsSellable { get; set; }
    public bool AllowFractionalQuantity { get; set; }
}

public sealed class TenantAdminProductBarcodeAddRequest
{
    public string Barcode { get; set; } = string.Empty;
}
