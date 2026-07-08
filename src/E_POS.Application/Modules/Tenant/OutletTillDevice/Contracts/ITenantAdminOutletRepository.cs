using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITenantAdminOutletRepository
{
    Task<bool> OutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);

    Task<TenantAdminOutletDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<TenantAdminOutletRevenueSummaryResponse> GetRevenueSummaryAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<TenantAdminOutletUsersResponse> GetUsersAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<TenantAdminOutletTillsResponse> GetTillsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);
}
