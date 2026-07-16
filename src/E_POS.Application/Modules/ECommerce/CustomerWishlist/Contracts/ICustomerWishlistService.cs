using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;

public interface ICustomerWishlistService
{
    Task<ApplicationResult<CustomerWishlistReadModel>> GetAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerWishlistReadModel>> AddItemAsync(
        Guid tenantId,
        Guid customerId,
        AddCustomerWishlistItemRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerWishlistReadModel>> RemoveItemAsync(
        Guid tenantId,
        Guid customerId,
        Guid itemId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerWishlistReadModel>> ClearAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);
}
