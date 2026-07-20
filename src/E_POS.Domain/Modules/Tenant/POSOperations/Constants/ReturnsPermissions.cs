namespace E_POS.Domain.Modules.Tenant.POSOperations.Constants;

/// <summary>
/// Canonical POS Returns / Refunds / Exchanges permission codes.
/// Backend and database remain the source of truth; Flutter must mirror these codes.
/// </summary>
public static class ReturnsPermissions
{
    public const string ViewReturns = "returns.view";
    public const string CreateReturn = "returns.create";
    public const string ViewRefunds = "refunds.view";
    public const string CreateRefund = "refunds.create";
    public const string ViewExchanges = "exchanges.view";
    public const string CreateExchange = "exchanges.create";
    public const string ApproveRefund = "pos.refund.approve";
}
