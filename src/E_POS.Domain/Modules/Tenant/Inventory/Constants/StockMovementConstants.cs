namespace E_POS.Domain.Modules.Tenant.Inventory.Constants;

public static class StockMovementConstants
{
    public const string StockIn = "STOCK_IN";
    public const string StockOut = "STOCK_OUT";
    public const string Adjustment = "ADJUSTMENT";
    public const string Transfer = "TRANSFER";

    public static readonly string[] StockInAliases =
    [
        StockIn,
        "stock_in",
        "INBOUND",
        "RECEIPT",
    ];

    public static readonly string[] StockOutAliases =
    [
        StockOut,
        "stock_out",
        "OUTBOUND",
        "ISSUE",
    ];

    public static readonly string[] AdjustmentAliases =
    [
        Adjustment,
        "adjustment",
        "ADJUST",
    ];

    public static readonly string[] TransferAliases =
    [
        Transfer,
        "transfer",
        "TRANSFER_OUT",
        "TRANSFER_IN",
    ];
}
