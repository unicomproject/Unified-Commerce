namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed class OpeningStockDraftDto
{
    public List<OpeningStockOwnerDraftDto> StockOwners { get; set; } = new();
}

public sealed class OpeningStockOwnerDraftDto
{
    public Guid? VariantId { get; set; }
    public decimal OpeningQuantity { get; set; }
    public List<OutletAllocationDraftDto> Allocations { get; set; } = new();
}

public sealed class OutletAllocationDraftDto
{
    public Guid OutletId { get; set; }
    public decimal Quantity { get; set; }
}
