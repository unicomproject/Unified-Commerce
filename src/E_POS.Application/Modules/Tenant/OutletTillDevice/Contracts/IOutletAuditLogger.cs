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
}
