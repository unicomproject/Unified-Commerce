using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IOutletRepository
{
    Task<bool> OutletCodeExistsAsync(Guid tenantId, string outletCode, Guid? excludeOutletId, CancellationToken cancellationToken);
    Task<Guid?> GetActivePickupFulfillmentMethodIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<OutletListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<OutletResponse?> GetByIdAsync(Guid tenantId, Guid outletId, bool includeDeleted, CancellationToken cancellationToken);
    Task<OutletEditAggregate?> GetEditAggregateAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);
    Task<bool> HasActiveTillOrDeviceAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);
    Task<bool> AllOutletsBelongToTenantAsync(Guid tenantId, Guid[] outletIds, CancellationToken cancellationToken);
    Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> IsOutletManagementFeatureEnabledAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> IsClickCollectFeatureEnabledAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<OutletCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> AddAsync(Outlet outlet, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? pickupMapping, CancellationToken cancellationToken);
    Task<bool> SaveUpdatedAsync(OutletEditAggregate aggregate, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? newPickupMapping, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record OutletEditAggregate(Outlet Outlet, OutletAddress? PhysicalAddress, IReadOnlyList<OutletBusinessHour> BusinessHours, FulfillmentMethodOutlet? PickupMapping);
