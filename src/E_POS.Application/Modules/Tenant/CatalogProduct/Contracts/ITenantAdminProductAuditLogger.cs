namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductAuditLogger
{
    void LogProductDeleted(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string outcome,
        string status);

    void LogStep2DraftUpdated(
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
        long rowVersion);
}
