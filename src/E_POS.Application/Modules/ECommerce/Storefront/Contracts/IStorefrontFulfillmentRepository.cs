using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontFulfillmentRepository
{
    Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<StorefrontCollectionConfigurationReadModel?> GetCollectionConfigurationAsync(
        Guid tenantId,
        Guid outletId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
