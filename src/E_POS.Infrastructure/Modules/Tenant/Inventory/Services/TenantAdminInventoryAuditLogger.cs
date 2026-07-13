using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.Inventory.Services;

public sealed class TenantAdminInventoryAuditLogger : ITenantAdminInventoryAuditLogger
{
    private readonly ILogger<TenantAdminInventoryAuditLogger> _logger;

    public TenantAdminInventoryAuditLogger(ILogger<TenantAdminInventoryAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogStockInCompleted(
        Guid tenantId,
        Guid userId,
        Guid operationId,
        Guid outletId,
        string? referenceNumber,
        int itemCount,
        decimal totalQuantity,
        IReadOnlyCollection<Guid> productVariantIds)
    {
        _logger.LogInformation(
            "StockInCompleted TenantId={TenantId} UserId={UserId} OperationId={OperationId} OutletId={OutletId} ReferenceNumber={ReferenceNumber} ItemCount={ItemCount} TotalQuantity={TotalQuantity} ProductVariantIds={ProductVariantIds}",
            tenantId,
            userId,
            operationId,
            outletId,
            referenceNumber,
            itemCount,
            totalQuantity,
            string.Join(',', productVariantIds));
    }
}
