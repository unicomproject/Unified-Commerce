using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Services;

public sealed class StorefrontCheckoutService : IStorefrontCheckoutService
{
    private const int MaximumSessionLength = 120;
    // sales_orders.external_order_reference is varchar(100); the checkout/session prefix uses 42 characters.
    private const int MaximumIdempotencyKeyLength = 50;
    private readonly IStorefrontCheckoutRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StorefrontCheckoutService(
        IStorefrontCheckoutRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<StorefrontCheckoutReadModel>> CreateFromCartAsync(
        Guid tenantId,
        Guid customerId,
        string? cartSessionId,
        CreateStorefrontCheckoutFromCartRequest request,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null) return Failure(contextError);

        var normalizedSession = cartSessionId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSession) || normalizedSession.Length > MaximumSessionLength)
            return Failure(Error("storefront_checkout.invalid_cart_session", "A valid X-Cart-Session-Id header is required."));
        if (request.SelectedOutletId == Guid.Empty)
            return Failure(Error("storefront_checkout.invalid_outlet_id", "A valid pickup outlet is required."));
        var contactError = ValidateContact(request);
        if (contactError is not null) return Failure(contactError);

        return Map(await _repository.CreateFromCartAsync(
            tenantId,
            customerId,
            normalizedSession,
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken));
    }

    public async Task<ApplicationResult<StorefrontCheckoutReadModel>> GetAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null) return Failure(contextError);
        if (checkoutSessionId == Guid.Empty)
            return Failure(Error("storefront_checkout.invalid_session_id", "A valid checkout session id is required."));

        return Map(await _repository.GetAsync(
            tenantId,
            customerId,
            checkoutSessionId,
            _dateTimeProvider.UtcNow,
            cancellationToken));
    }

    public async Task<ApplicationResult<StorefrontCheckoutReadModel>> UpdateCollectionAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        UpdateStorefrontCheckoutCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null) return Failure(contextError);
        if (checkoutSessionId == Guid.Empty)
            return Failure(Error("storefront_checkout.invalid_session_id", "A valid checkout session id is required."));
        if (request.SelectedOutletId == Guid.Empty)
            return Failure(Error("storefront_checkout.invalid_outlet_id", "A valid pickup outlet is required."));
        if (request.RequestedCollectionAt == default)
            return Failure(Error("storefront_checkout.invalid_collection_time", "A valid collection time is required."));

        return Map(await _repository.UpdateCollectionAsync(
            tenantId,
            customerId,
            checkoutSessionId,
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken));
    }

    public async Task<ApplicationResult<StorefrontCheckoutReadModel>> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var contextError = ValidateCustomerContext(tenantId, customerId);
        if (contextError is not null) return Failure(contextError);
        if (checkoutSessionId == Guid.Empty)
            return Failure(Error("storefront_checkout.invalid_session_id", "A valid checkout session id is required."));

        var normalizedKey = idempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey) || normalizedKey.Length > MaximumIdempotencyKeyLength)
            return Failure(Error("storefront_checkout.invalid_idempotency_key", "A valid Idempotency-Key header is required."));

        return Map(await _repository.ConfirmAsync(
            tenantId,
            customerId,
            checkoutSessionId,
            normalizedKey,
            _dateTimeProvider.UtcNow,
            cancellationToken));
    }

    private static ApplicationError? ValidateCustomerContext(Guid tenantId, Guid customerId) =>
        tenantId == Guid.Empty || customerId == Guid.Empty
            ? Error("storefront_checkout.invalid_customer_context", "A valid customer session is required.")
            : null;

    private static ApplicationError? ValidateContact(CreateStorefrontCheckoutFromCartRequest request)
    {
        if (request.PickupContactName?.Trim().Length > 150)
            return Error("storefront_checkout.invalid_contact", "Pickup contact name must not exceed 150 characters.");
        if (request.PickupContactPhone?.Trim().Length > 50)
            return Error("storefront_checkout.invalid_contact", "Pickup contact phone must not exceed 50 characters.");
        if (request.PickupContactEmail?.Trim().Length > 255)
            return Error("storefront_checkout.invalid_contact", "Pickup contact email must not exceed 255 characters.");
        if (!string.IsNullOrWhiteSpace(request.PickupContactEmail) &&
            !request.PickupContactEmail.Contains('@', StringComparison.Ordinal))
            return Error("storefront_checkout.invalid_contact", "Pickup contact email is invalid.");
        return null;
    }

    private static ApplicationResult<StorefrontCheckoutReadModel> Map(
        StorefrontCheckoutRepositoryResult result) =>
        result.IsSuccess
            ? ApplicationResult<StorefrontCheckoutReadModel>.Success(result.Checkout!)
            : Failure(MapError(result.ErrorCode!));

    private static ApplicationError MapError(string code) => code switch
    {
        "storefront_checkout.tenant_unavailable" => Error(code, "The storefront tenant is unavailable."),
        "storefront_checkout.feature_disabled" => Error(code, "Click & Collect is not enabled for this tenant."),
        "storefront_checkout.customer_not_found" => Error(code, "Customer not found or unavailable."),
        "storefront_checkout.outlet_not_found" => Error(code, "Pickup outlet not found or unavailable."),
        "storefront_checkout.cart_not_found" => Error(code, "Active cart not found or access denied."),
        "storefront_checkout.cart_empty" => Error(code, "The cart is empty."),
        "storefront_checkout.product_unavailable" => Error(code, "A cart product is no longer available."),
        "storefront_checkout.variant_unavailable" => Error(code, "A cart product variant is no longer available."),
        "storefront_checkout.price_not_configured" => Error(code, "A current selling price is not configured for a cart item."),
        "storefront_checkout.insufficient_stock" => Error(code, "One or more cart items are not available at the selected outlet."),
        "storefront_checkout.collection_configuration_missing" => Error(code, "Collection is not configured for the selected outlet."),
        "storefront_checkout.collection_time_unavailable" => Error(code, "The requested collection time is unavailable."),
        "storefront_checkout.invalid_outlet_timezone" => Error(code, "The selected outlet timezone is invalid."),
        "storefront_checkout.collection_required" => Error(code, "Select a valid collection date and time before confirming."),
        "storefront_checkout.session_not_found" => Error(code, "Checkout session not found or access denied."),
        "storefront_checkout.session_expired" => Error(code, "The checkout session has expired."),
        "storefront_checkout.invalid_state" => Error(code, "The checkout session cannot be confirmed in its current state."),
        "storefront_checkout.uom_not_configured" => Error(code, "A sales unit of measure is not configured for a cart item."),
        "storefront_checkout.sales_channel_not_configured" => Error(code, "The online sales channel is not configured."),
        _ => Error(code, "The checkout operation could not be completed.")
    };

    private static ApplicationResult<StorefrontCheckoutReadModel> Failure(ApplicationError error) =>
        ApplicationResult<StorefrontCheckoutReadModel>.Failure(error);

    private static ApplicationError Error(string code, string message) => new(code, message);
}
