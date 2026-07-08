using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITenantAdminTillRepository
{
    Task<bool> OutletBelongsToTenantAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<bool> TillCodeExistsForTenantAsync(
        Guid tenantId,
        string tillCode,
        Guid? excludeTillId,
        CancellationToken cancellationToken);

    Task<int> GetNextTillNumberAsync(
        Guid tenantId,
        Guid outletId,
        string tillAreaName,
        CancellationToken cancellationToken);

    Task<TenantAdminTillListResponse> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        Guid? outletId,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken);

    Task<TenantAdminTillSummaryResponse> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<TenantAdminTillDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken);

    Task AddAsync(Till till, CancellationToken cancellationToken);

    Task<Till?> GetEditableAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> HasActiveDeviceAssignmentAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveSessionAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken);

    Task<bool> HasSalesAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken);

    Task<bool> HasCashMovementsAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantAdminOutletOptionResponse>> GetOutletOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
