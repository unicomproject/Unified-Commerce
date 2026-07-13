namespace E_POS.Application.Modules.Tenant.Inventory.Contracts;

public interface ITenantAdminInventoryAuditLogger
{
    void LogStockInCompleted(
        Guid tenantId,
        Guid userId,
        Guid operationId,
        Guid outletId,
        string? referenceNumber,
        int itemCount,
        decimal totalQuantity,
        IReadOnlyCollection<Guid> productVariantIds);
}
