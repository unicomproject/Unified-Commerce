using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Services;

public sealed class InventoryAuditLogger : IInventoryAuditLogger
{
    private readonly ILogger<InventoryAuditLogger> _logger;

    public InventoryAuditLogger(ILogger<InventoryAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogStockInCompleted(Guid tenantId, Guid userId, Guid stockMovementId, Guid outletId)
    {
        _logger.LogInformation(
            "Stock-in completed successfully for TenantId: {TenantId}, UserId: {UserId}, StockMovementId: {StockMovementId}, OutletId: {OutletId}",
            tenantId, userId, stockMovementId, outletId);
    }
}
