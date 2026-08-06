using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Services;

public sealed class OutletAuditLogger : IOutletAuditLogger
{
    private readonly ILogger<OutletAuditLogger> _logger;

    public OutletAuditLogger(ILogger<OutletAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogOutletCreated(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        string outletCode,
        string outletType,
        string status)
    {
        _logger.LogInformation(
            "OUTLET_CREATED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} OutletCode={OutletCode} OutletType={OutletType} Status={Status}",
            tenantId,
            actorTenantUserId,
            outletId,
            outletCode,
            outletType,
            status);
    }

    public void LogManagerAssigned(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid managerTenantUserId)
    {
        _logger.LogInformation(
            "OUTLET_MANAGER_ASSIGNED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} ManagerTenantUserId={ManagerTenantUserId}",
            tenantId,
            actorTenantUserId,
            outletId,
            managerTenantUserId);
    }

    public void LogManagerRemoved(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId)
    {
        _logger.LogInformation(
            "OUTLET_MANAGER_REMOVED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId}",
            tenantId,
            actorTenantUserId,
            outletId);
    }

    public void LogImageAssociated(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid mediaAssetId)
    {
        _logger.LogInformation(
            "OUTLET_IMAGE_ASSOCIATED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} MediaAssetId={MediaAssetId}",
            tenantId,
            actorTenantUserId,
            outletId,
            mediaAssetId);
    }

    public void LogImageRemoved(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId)
    {
        _logger.LogInformation(
            "OUTLET_IMAGE_REMOVED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId}",
            tenantId,
            actorTenantUserId,
            outletId);
    }

    public void LogStatusChanged(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        string status)
    {
        _logger.LogInformation(
            "OUTLET_STATUS_CHANGED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} Status={Status}",
            tenantId,
            actorTenantUserId,
            outletId,
            status);
    }
}
