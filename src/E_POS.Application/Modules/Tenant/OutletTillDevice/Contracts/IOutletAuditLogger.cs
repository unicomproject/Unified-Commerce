namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IOutletAuditLogger
{
    void LogOutletCreated(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        string outletCode,
        string outletType,
        string status);

    void LogManagerAssigned(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid managerTenantUserId);

    void LogManagerRemoved(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId);

    void LogImageAssociated(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid mediaAssetId);

    void LogImageRemoved(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId);

    void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid mediaAssetId);

    void LogImageReplaced(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid previousMediaAssetId,
        Guid newMediaAssetId);

    void LogImageDetached(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid detachedMediaAssetId);

    void LogStatusChanged(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        string status);
}
