namespace E_POS.Domain.Modules.Tenant.Inventory.Constants;

public static class TenantAdminStockPermissions
{
    public const string View = "tenant.stock.view";
    public const string StockIn = "tenant.stock.in";
    public const string StockOut = "tenant.stock.out";
    public const string ValueView = "tenant.stock.value.view";
    public const string MovementsView = "tenant.stock.movements.view";
    public const string ExpiryView = "tenant.stock.expiry.view";
    public const string AdjustmentsView = "tenant.stock.adjustments.view";
    public const string TransfersView = "tenant.stock.transfers.view";

    public const string LegacyInventoryView = "inventory.stock.view";
}
