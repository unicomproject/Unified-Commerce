using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontFulfillmentService
{
    Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ApplicationResult<StorefrontCollectionOptionsReadModel>> GetCollectionOptionsAsync(
        Guid tenantId,
        Guid outletId,
        int days,
        CancellationToken cancellationToken = default);
}
