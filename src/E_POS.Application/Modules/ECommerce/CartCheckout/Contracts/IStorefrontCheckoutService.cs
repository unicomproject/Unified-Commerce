using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;

public interface IStorefrontCheckoutService
{
    Task<ApplicationResult<StorefrontCheckoutReadModel>> CreateFromCartAsync(
        Guid tenantId,
        Guid customerId,
        string? cartSessionId,
        CreateStorefrontCheckoutFromCartRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StorefrontCheckoutReadModel>> GetAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StorefrontCheckoutReadModel>> UpdateCollectionAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        UpdateStorefrontCheckoutCollectionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StorefrontCheckoutReadModel>> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}
