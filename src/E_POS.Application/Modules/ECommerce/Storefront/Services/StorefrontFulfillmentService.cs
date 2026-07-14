using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontFulfillmentService : IStorefrontFulfillmentService
{
    private readonly IStorefrontFulfillmentRepository _repository;

    public StorefrontFulfillmentService(IStorefrontFulfillmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetAvailableStoresAsync(tenantId, cancellationToken);
    }
}