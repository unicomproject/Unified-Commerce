using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerWishlist.Services;

public sealed class CustomerWishlistService : ICustomerWishlistService
{
    private readonly ICustomerWishlistRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerWishlistService(
        ICustomerWishlistRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<ApplicationResult<CustomerWishlistReadModel>> GetAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            tenantId,
            customerId,
            now => _repository.GetAsync(tenantId, customerId, now, cancellationToken));

    public Task<ApplicationResult<CustomerWishlistReadModel>> AddItemAsync(
        Guid tenantId,
        Guid customerId,
        AddCustomerWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
            return Failure("customer_wishlist.invalid_product_id", "A valid product id is required.");

        return ExecuteAsync(
            tenantId,
            customerId,
            now => _repository.AddItemAsync(
                tenantId,
                customerId,
                request,
                now,
                cancellationToken));
    }

    public Task<ApplicationResult<CustomerWishlistReadModel>> RemoveItemAsync(
        Guid tenantId,
        Guid customerId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (itemId == Guid.Empty)
            return Failure("customer_wishlist.invalid_item_id", "A valid wishlist item id is required.");

        return ExecuteAsync(
            tenantId,
            customerId,
            now => _repository.RemoveItemAsync(
                tenantId,
                customerId,
                itemId,
                now,
                cancellationToken));
    }

    public Task<ApplicationResult<CustomerWishlistReadModel>> ClearAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            tenantId,
            customerId,
            now => _repository.ClearAsync(tenantId, customerId, now, cancellationToken));

    private async Task<ApplicationResult<CustomerWishlistReadModel>> ExecuteAsync(
        Guid tenantId,
        Guid customerId,
        Func<DateTimeOffset, Task<CustomerWishlistRepositoryResult>> operation)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty)
        {
            return ApplicationResult<CustomerWishlistReadModel>.Failure(
                Error("customer_wishlist.invalid_customer_context", "A valid customer session is required."));
        }

        var result = await operation(_dateTimeProvider.UtcNow);
        return result.IsSuccess
            ? ApplicationResult<CustomerWishlistReadModel>.Success(result.Wishlist!)
            : ApplicationResult<CustomerWishlistReadModel>.Failure(MapError(result.ErrorCode!));
    }

    private static Task<ApplicationResult<CustomerWishlistReadModel>> Failure(
        string code,
        string message) =>
        Task.FromResult(ApplicationResult<CustomerWishlistReadModel>.Failure(Error(code, message)));

    private static ApplicationError MapError(string code) => code switch
    {
        "customer_wishlist.customer_not_found" => Error(code, "Customer not found or unavailable."),
        "customer_wishlist.product_not_found" => Error(code, "Product not found or unavailable."),
        "customer_wishlist.variant_not_found" => Error(code, "Product variant not found or unavailable."),
        "customer_wishlist.item_not_found" => Error(code, "Wishlist item not found or access denied."),
        _ => Error(code, "The wishlist operation could not be completed.")
    };

    private static ApplicationError Error(string code, string message) => new(code, message);
}
