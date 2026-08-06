using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITenantAdminOutletRepository
{
    Task<TenantAdminOutletListResponse> ListAsync(
        Guid tenantId,
        TenantAdminOutletListQuery query,
        CancellationToken cancellationToken);

    Task<bool> OutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);

    Task<TenantAdminOutletLifecycleState?> GetLifecycleStateAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(
        Guid tenantId,
        Guid outletId,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

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

    Task<OutletOverviewInfoResponse?> GetOverviewInfoAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<OutletOverviewManagerResponse?> GetOverviewManagerAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<OutletOverviewSalesSummaryResponse> GetOverviewSalesAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<decimal> GetOverviewStockValueAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<int> GetOverviewOpenOrdersCountAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OutletOperationalHealthCalculator.TillHealthInput>> GetOverviewTillHealthInputsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<string> GetTenantCurrencyCodeAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<bool> TenantUserExistsAndActiveAsync(
        Guid tenantId,
        Guid tenantUserId,
        CancellationToken cancellationToken);

    Task<bool> MediaAssetExistsAndActiveAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    Task<bool> SetPrimaryManagerAsync(
        Guid tenantId,
        Guid outletId,
        Guid tenantUserId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> RemovePrimaryManagerAsync(
        Guid tenantId,
        Guid outletId,
        Guid? revokedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> SetOutletImageAsync(
        Guid tenantId,
        Guid outletId,
        Guid mediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> RemoveOutletImageAsync(
        Guid tenantId,
        Guid outletId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
