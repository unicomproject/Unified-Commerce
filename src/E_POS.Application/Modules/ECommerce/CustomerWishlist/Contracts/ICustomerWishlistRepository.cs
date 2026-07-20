using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;

public interface ICustomerWishlistRepository
{
    Task<CustomerWishlistRepositoryResult> GetAsync(
        Guid tenantId,
        Guid customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CustomerWishlistRepositoryResult> AddItemAsync(
        Guid tenantId,
        Guid customerId,
        AddCustomerWishlistItemRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CustomerWishlistRepositoryResult> RemoveItemAsync(
        Guid tenantId,
        Guid customerId,
        Guid itemId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CustomerWishlistRepositoryResult> ClearAsync(
        Guid tenantId,
        Guid customerId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record CustomerWishlistRepositoryResult(
    string? ErrorCode,
    CustomerWishlistReadModel? Wishlist)
{
    public bool IsSuccess => ErrorCode is null && Wishlist is not null;

    public static CustomerWishlistRepositoryResult Success(CustomerWishlistReadModel wishlist) =>
        new(null, wishlist);

    public static CustomerWishlistRepositoryResult Failure(string errorCode) =>
        new(errorCode, null);
}
