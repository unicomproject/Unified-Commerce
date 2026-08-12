namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantUserStaffCodeService
{
    Task<string> GenerateAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken);
}
