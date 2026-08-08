using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Services;

public sealed class OutletAuditLogger : IOutletAuditLogger
{
    private readonly ILogger<OutletAuditLogger> _logger;
    private readonly EPosDbContext _dbContext;

    public OutletAuditLogger(ILogger<OutletAuditLogger> logger, EPosDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public void LogOutletCreated(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        string outletCode,
        string outletType,
        string status)
    {
        Persist(tenantId, actorTenantUserId, outletId, "outlet.created", $"{{\"outletCode\":\"{outletCode}\",\"outletType\":\"{outletType}\",\"status\":\"{status}\"}}");
        _logger.LogInformation(
            "OUTLET_CREATED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} OutletCode={OutletCode} OutletType={OutletType} Status={Status}",
            tenantId,
            actorTenantUserId,
            outletId,
            outletCode,
            outletType,
            status);
    }

    public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid mediaAssetId)
    {
        Persist(tenantId, actorTenantUserId, mediaAssetId, "outlet.image_uploaded", null);
        _logger.LogInformation("OUTLET_IMAGE_UPLOADED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} MediaAssetId={MediaAssetId}", tenantId, actorTenantUserId, mediaAssetId);
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
        Persist(tenantId, actorTenantUserId, outletId, "outlet.image_attached", $"{{\"mediaAssetId\":\"{mediaAssetId:D}\"}}");
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
        Persist(tenantId, actorTenantUserId, outletId, "outlet.image_removed", null);
        _logger.LogInformation(
            "OUTLET_IMAGE_REMOVED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId}",
            tenantId,
            actorTenantUserId,
            outletId);
    }

    public void LogImageReplaced(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid previousMediaAssetId,
        Guid newMediaAssetId)
    {
        Persist(tenantId, actorTenantUserId, outletId, "outlet.image_replaced", $"{{\"previousMediaAssetId\":\"{previousMediaAssetId:D}\",\"newMediaAssetId\":\"{newMediaAssetId:D}\"}}");
        _logger.LogInformation(
            "OUTLET_IMAGE_REPLACED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} PreviousMediaAssetId={PreviousMediaAssetId} NewMediaAssetId={NewMediaAssetId}",
            tenantId,
            actorTenantUserId,
            outletId,
            previousMediaAssetId,
            newMediaAssetId);
    }

    public void LogImageDetached(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid outletId,
        Guid detachedMediaAssetId)
    {
        Persist(tenantId, actorTenantUserId, outletId, "outlet.image_detached", $"{{\"mediaAssetId\":\"{detachedMediaAssetId:D}\"}}");
        _logger.LogInformation(
            "OUTLET_IMAGE_DETACHED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=OUTLET EntityId={EntityId} DetachedMediaAssetId={DetachedMediaAssetId}",
            tenantId,
            actorTenantUserId,
            outletId,
            detachedMediaAssetId);
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

    private void Persist(Guid tenantId, Guid actorTenantUserId, Guid entityId, string action, string? newValues)
    {
        _dbContext.AuditLogs.Add(new AuditLog { TenantId = tenantId, ActorUserId = actorTenantUserId, ActorType = "TENANT_USER", EntityType = "OUTLET", EntityId = entityId, Action = action, NewValues = newValues, CreatedAt = DateTimeOffset.UtcNow });
        _dbContext.SaveChanges();
    }
}
