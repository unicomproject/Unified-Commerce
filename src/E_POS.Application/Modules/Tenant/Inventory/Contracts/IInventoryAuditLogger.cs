namespace E_POS.Application.Modules.Tenant.Inventory.Contracts;

public interface IInventoryAuditLogger
{
    void LogStockInCompleted(Guid tenantId, Guid userId, Guid stockMovementId, Guid outletId);
}
