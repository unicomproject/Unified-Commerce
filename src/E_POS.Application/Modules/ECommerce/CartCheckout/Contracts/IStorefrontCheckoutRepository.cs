using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;

public interface IStorefrontCheckoutRepository
{
    Task<StorefrontCheckoutRepositoryResult> CreateFromCartAsync(
        Guid tenantId,
        Guid customerId,
        string cartSessionId,
        CreateStorefrontCheckoutFromCartRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<StorefrontCheckoutRepositoryResult> GetAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<StorefrontCheckoutRepositoryResult> UpdateCollectionAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        UpdateStorefrontCheckoutCollectionRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<StorefrontCheckoutRepositoryResult> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string idempotencyKey,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record StorefrontCheckoutRepositoryResult(
    string? ErrorCode,
    StorefrontCheckoutReadModel? Checkout)
{
    public bool IsSuccess => ErrorCode is null && Checkout is not null;

    public static StorefrontCheckoutRepositoryResult Success(StorefrontCheckoutReadModel checkout) =>
        new(null, checkout);

    public static StorefrontCheckoutRepositoryResult Failure(string errorCode) =>
        new(errorCode, null);
}
