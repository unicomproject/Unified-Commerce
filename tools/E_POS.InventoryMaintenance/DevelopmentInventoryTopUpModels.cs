namespace E_POS.InventoryMaintenance;

public sealed record DevelopmentInventoryTopUpOptions(
    string TenantCode,
    string OutletCode,
    string LocationCode,
    string ActorEmail,
    decimal TargetMinimum);

public sealed record DevelopmentInventoryContext(
    Guid TenantId,
    string TenantCode,
    string TenantName,
    string TenantStatus,
    Guid OutletId,
    string OutletCode,
    string OutletName,
    string OutletStatus,
    Guid? DeviceId,
    string? DeviceName,
    Guid? TillId,
    IReadOnlyList<DevelopmentInventoryLocation> Locations,
    IReadOnlyList<string> ActiveTenantUsers);

public sealed record DevelopmentInventoryLocation(
    Guid Id,
    string Code,
    string Name,
    string Status,
    bool IsSellable,
    bool IsReceiving,
    int BalanceCount,
    decimal TotalOnHand);

public sealed record DevelopmentInventoryTopUpItem(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid ProductVariantId,
    string VariantCode,
    string VariantName,
    decimal QuantityBefore,
    decimal QuantityChange,
    decimal QuantityAfter,
    Guid? InventoryBalanceId,
    Guid? StockAdjustmentLineId,
    Guid? StockMovementId,
    string Outcome);

public sealed record DevelopmentInventoryTopUpResult(
    Guid TenantId,
    string TenantCode,
    Guid OutletId,
    string OutletCode,
    Guid InventoryLocationId,
    string LocationCode,
    Guid ActorUserId,
    string ActorEmail,
    decimal TargetMinimum,
    int ProductsInspected,
    int VariantsInspected,
    int VariantsToppedUp,
    int AlreadySufficient,
    int SkippedNonStockTracked,
    int SkippedBatchTracked,
    int SkippedSerialTracked,
    int MissingBalancesCreated,
    Guid? StockAdjustmentId,
    string? AdjustmentNumber,
    string ReasonCode,
    int AdjustmentLinesCreated,
    int StockMovementsCreated,
    IReadOnlyList<DevelopmentInventoryTopUpItem> Items);

public static class DevelopmentInventoryTopUpPolicy
{
    public static decimal CalculateQuantityChange(decimal currentOnHand, decimal targetMinimum)
    {
        if (currentOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentOnHand));
        }

        if (targetMinimum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetMinimum));
        }

        return Math.Max(0, targetMinimum - currentOnHand);
    }
}
