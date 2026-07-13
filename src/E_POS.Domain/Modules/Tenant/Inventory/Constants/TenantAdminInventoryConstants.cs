namespace E_POS.Domain.Modules.Tenant.Inventory.Constants;

public static class TenantAdminInventoryConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string DefaultLocationType = "STORE";
    public const string StockMovementSequenceKey = "STOCK_MOVEMENT_NUMBER";
    public const string StockMovementPrefix = "SM";
    public const int StockMovementPadding = 6;
    public const string StockReceiptReferenceType = "STOCK_IN_RECEIPT";
    public const string CostLayerActiveStatus = "ACTIVE";
    public const string StockInCompletedStatus = "COMPLETED";

    public const string StockStatusInStock = "IN_STOCK";
    public const string StockStatusLowStock = "LOW_STOCK";
    public const string StockStatusOutOfStock = "OUT_OF_STOCK";
    public const string StockStatusFilterAll = "ALL";

    public const string ExpiryStatusNotApplicable = "NOT_APPLICABLE";
    public const string ExpiryStatusValid = "VALID";
    public const string ExpiryStatusExpiringSoon = "EXPIRING_SOON";
    public const string ExpiryStatusExpired = "EXPIRED";

    public const string ExpiryFilterExpiring = "EXPIRING";
    public const string ExpiryFilterExpired = "EXPIRED";
    public const string ExpiryFilterAll = "ALL";
}
