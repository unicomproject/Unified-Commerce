using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;

public sealed class TenantAdminProductAuditLogger : ITenantAdminProductAuditLogger
{
    private readonly ILogger<TenantAdminProductAuditLogger> _logger;

    public TenantAdminProductAuditLogger(ILogger<TenantAdminProductAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogProductDeleted(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string outcome,
        string status)
    {
        _logger.LogInformation(
            "ProductDeleted TenantId={TenantId} UserId={UserId} ProductId={ProductId} Outcome={Outcome} Status={Status}",
            tenantId,
            userId,
            productId,
            outcome,
            status);
    }

    public void LogStep2DraftUpdated(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string oldStructure,
        string newStructure,
        bool oldTrackInventory,
        bool newTrackInventory,
        bool oldBatchTracking,
        bool newBatchTracking,
        bool oldExpiryTracking,
        bool newExpiryTracking,
        bool oldSerialTracking,
        bool newSerialTracking,
        long rowVersion)
    {
        _logger.LogInformation(
            "PRODUCT_DRAFT_STEP2_UPDATED TenantId={TenantId} UserId={UserId} ProductId={ProductId} OldStructure={OldStructure} NewStructure={NewStructure} OldTrackInventory={OldTrackInventory} NewTrackInventory={NewTrackInventory} OldBatch={OldBatch} NewBatch={NewBatch} OldExpiry={OldExpiry} NewExpiry={NewExpiry} OldSerial={OldSerial} NewSerial={NewSerial} RowVersion={RowVersion}",
            tenantId,
            userId,
            productId,
            oldStructure,
            newStructure,
            oldTrackInventory,
            newTrackInventory,
            oldBatchTracking,
            newBatchTracking,
            oldExpiryTracking,
            newExpiryTracking,
            oldSerialTracking,
            newSerialTracking,
            rowVersion);
    }
}
