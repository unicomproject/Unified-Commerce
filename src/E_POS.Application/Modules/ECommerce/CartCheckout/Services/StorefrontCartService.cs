using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Services;

public sealed class StorefrontCartService : IStorefrontCartService
{
    private readonly IStorefrontCartRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StorefrontCartService(IStorefrontCartRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<ApplicationResult<StorefrontCartReadModel>> GetAsync(
        Guid tenantId, string? sessionId, CancellationToken cancellationToken) =>
        ExecuteAsync(tenantId, sessionId, null,
            (normalizedSession, now) => _repository.GetAsync(tenantId, normalizedSession, now, cancellationToken));

    public Task<ApplicationResult<StorefrontCartReadModel>> AddItemAsync(
        Guid tenantId,
        string? sessionId,
        AddStorefrontCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
            return Failure("storefront_cart.invalid_product_id", "A valid product id is required.");
        if (!IsValidQuantity(request.Quantity))
            return Failure("storefront_cart.invalid_quantity", "Quantity must be greater than zero and no more than 9999.");

        return ExecuteAsync(tenantId, sessionId, request,
            (normalizedSession, now) => _repository.AddItemAsync(
                tenantId, normalizedSession, request, now, cancellationToken));
    }

    public Task<ApplicationResult<StorefrontCartReadModel>> UpdateItemAsync(
        Guid tenantId,
        string? sessionId,
        Guid itemId,
        UpdateStorefrontCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (itemId == Guid.Empty)
            return Failure("storefront_cart.invalid_item_id", "A valid cart item id is required.");
        if (!IsValidQuantity(request.Quantity))
            return Failure("storefront_cart.invalid_quantity", "Quantity must be greater than zero and no more than 9999.");

        return ExecuteAsync(tenantId, sessionId, request,
            (normalizedSession, now) => _repository.UpdateItemAsync(
                tenantId, normalizedSession, itemId, request.Quantity, now, cancellationToken));
    }

    public Task<ApplicationResult<StorefrontCartReadModel>> RemoveItemAsync(
        Guid tenantId, string? sessionId, Guid itemId, CancellationToken cancellationToken)
    {
        if (itemId == Guid.Empty)
            return Failure("storefront_cart.invalid_item_id", "A valid cart item id is required.");

        return ExecuteAsync(tenantId, sessionId, itemId,
            (normalizedSession, now) => _repository.RemoveItemAsync(
                tenantId, normalizedSession, itemId, now, cancellationToken));
    }

    public Task<ApplicationResult<StorefrontCartReadModel>> ClearAsync(
        Guid tenantId, string? sessionId, CancellationToken cancellationToken) =>
        ExecuteAsync(tenantId, sessionId, null,
            (normalizedSession, now) => _repository.ClearAsync(
                tenantId, normalizedSession, now, cancellationToken));

    private async Task<ApplicationResult<StorefrontCartReadModel>> ExecuteAsync(
        Guid tenantId,
        string? sessionId,
        object? _,
        Func<string, DateTimeOffset, Task<StorefrontCartRepositoryResult>> operation)
    {
        if (tenantId == Guid.Empty)
            return ApplicationResult<StorefrontCartReadModel>.Failure(
                Error("storefront_cart.invalid_tenant_context", "A valid tenant context is required."));

        var normalizedSession = sessionId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSession) || normalizedSession.Length > 120)
            return ApplicationResult<StorefrontCartReadModel>.Failure(
                Error("storefront_cart.invalid_session", "X-Cart-Session-Id is required and must not exceed 120 characters."));

        var result = await operation(normalizedSession, _dateTimeProvider.UtcNow);
        return result.IsSuccess
            ? ApplicationResult<StorefrontCartReadModel>.Success(result.Cart!)
            : ApplicationResult<StorefrontCartReadModel>.Failure(MapError(result.ErrorCode!));
    }

    private static bool IsValidQuantity(decimal quantity) => quantity > 0m && quantity <= 9999m;

    private static Task<ApplicationResult<StorefrontCartReadModel>> Failure(string code, string message) =>
        Task.FromResult(ApplicationResult<StorefrontCartReadModel>.Failure(Error(code, message)));

    private static ApplicationError MapError(string code) => code switch
    {
        "storefront_cart.product_not_found" => Error(code, "Product not found or unavailable."),
        "storefront_cart.variant_not_found" => Error(code, "Product variant not found or unavailable."),
        "storefront_cart.variant_required" => Error(code, "A product variant must be selected."),
        "storefront_cart.price_not_configured" => Error(code, "A selling price is not configured for this product."),
        "storefront_cart.insufficient_stock" => Error(code, "The requested quantity is not available."),
        "storefront_cart.item_not_found" => Error(code, "Cart item not found or access denied."),
        "storefront_cart.expired" => Error(code, "The cart has expired."),
        _ => Error(code, "The cart operation could not be completed.")
    };

    private static ApplicationError Error(string code, string message) => new(code, message);
}
