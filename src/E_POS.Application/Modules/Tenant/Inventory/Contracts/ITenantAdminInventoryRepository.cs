using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.Inventory.Contracts;

public interface ITenantAdminInventoryRepository
{
    Task<IReadOnlyList<Guid>> GetAccessibleOutletIdsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<TenantAdminCurrentStockListResponse> GetCurrentStockAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminCurrentStockQuery query,
        CancellationToken cancellationToken);

    Task<TenantAdminCurrentStockSummaryResponse> GetCurrentStockSummaryAsync(
        Guid tenantId,
        Guid userId,
        Guid? outletId,
        CancellationToken cancellationToken);

    Task<TenantAdminStockInResponse> StockInAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminStockInRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantAdminInventoryVariantLookupResponse?> GetProductVariantsForStockInAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<bool> OutletExistsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<bool> UserHasOutletAccessAsync(
        Guid tenantId,
        Guid userId,
        Guid outletId,
        CancellationToken cancellationToken);

    Task<bool> StockInIdempotencyKeyExistsAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<TenantAdminInventoryTrackingProfile?> GetVariantTrackingProfileAsync(
        Guid tenantId,
        Guid variantId,
        CancellationToken cancellationToken);

    Task<string?> GetOutletNameAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);
}
