using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;

public interface IStorefrontFulfilmentRepository
{
    Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<StorefrontCollectionConfigurationReadModel?> GetCollectionConfigurationAsync(
        Guid tenantId,
        Guid outletId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
