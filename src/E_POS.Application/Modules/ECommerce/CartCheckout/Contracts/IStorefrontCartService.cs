using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;

public interface IStorefrontCartService
{
    Task<ApplicationResult<StorefrontCartReadModel>> GetAsync(Guid tenantId, string? sessionId, CancellationToken cancellationToken);
    Task<ApplicationResult<StorefrontCartReadModel>> AddItemAsync(Guid tenantId, string? sessionId, AddStorefrontCartItemRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<StorefrontCartReadModel>> UpdateItemAsync(Guid tenantId, string? sessionId, Guid itemId, UpdateStorefrontCartItemRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<StorefrontCartReadModel>> RemoveItemAsync(Guid tenantId, string? sessionId, Guid itemId, CancellationToken cancellationToken);
    Task<ApplicationResult<StorefrontCartReadModel>> ClearAsync(Guid tenantId, string? sessionId, CancellationToken cancellationToken);
}
