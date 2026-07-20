using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;

public interface IStorefrontCartRepository
{
    Task<StorefrontCartRepositoryResult> GetAsync(Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<StorefrontCartRepositoryResult> AddItemAsync(Guid tenantId, string sessionId, AddStorefrontCartItemRequest request, DateTimeOffset now, CancellationToken cancellationToken);
    Task<StorefrontCartRepositoryResult> UpdateItemAsync(Guid tenantId, string sessionId, Guid itemId, decimal quantity, DateTimeOffset now, CancellationToken cancellationToken);
    Task<StorefrontCartRepositoryResult> RemoveItemAsync(Guid tenantId, string sessionId, Guid itemId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<StorefrontCartRepositoryResult> ClearAsync(Guid tenantId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed record StorefrontCartRepositoryResult(string? ErrorCode, StorefrontCartReadModel? Cart)
{
    public bool IsSuccess => ErrorCode is null && Cart is not null;
    public static StorefrontCartRepositoryResult Success(StorefrontCartReadModel cart) => new(null, cart);
    public static StorefrontCartRepositoryResult Failure(string errorCode) => new(errorCode, null);
}
