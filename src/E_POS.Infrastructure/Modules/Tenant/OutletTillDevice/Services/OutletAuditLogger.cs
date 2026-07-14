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
}
