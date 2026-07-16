using System.Security.Claims;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v1/ecommerce/storefront/checkout")]
public sealed class StorefrontCheckoutController : ControllerBase
{
    private readonly IStorefrontCheckoutService _service;

    public StorefrontCheckoutController(IStorefrontCheckoutService service)
    {
        _service = service;
    }

    [HttpPost("from-cart")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFromCart(
        [FromBody] CreateStorefrontCheckoutFromCartRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _service.CreateFromCartAsync(
            tenantId,
            customerId,
            Request.Headers["X-Cart-Session-Id"].FirstOrDefault(),
            request,
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(
                nameof(Get),
                new { sessionId = result.Value.Id },
                new { success = true, message = "Checkout session created successfully.", data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _service.GetAsync(tenantId, customerId, sessionId, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { success = true, message = "Checkout session retrieved successfully.", data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPost("{sessionId:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid sessionId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _service.ConfirmAsync(
            tenantId,
            customerId,
            sessionId,
            idempotencyKey,
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { success = true, message = "Checkout confirmed successfully.", data = result.Value })
            : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        var response = CreateError(error);
        return error.Code switch
        {
            "storefront_checkout.invalid_customer_context" => Unauthorized(response),
            "storefront_checkout.customer_not_found" or
            "storefront_checkout.outlet_not_found" or
            "storefront_checkout.cart_not_found" or
            "storefront_checkout.session_not_found" => NotFound(response),
            "storefront_checkout.cart_empty" or
            "storefront_checkout.product_unavailable" or
            "storefront_checkout.variant_unavailable" or
            "storefront_checkout.price_not_configured" or
            "storefront_checkout.insufficient_stock" or
            "storefront_checkout.pickup_slot_unavailable" or
            "storefront_checkout.session_expired" or
            "storefront_checkout.invalid_state" or
            "storefront_checkout.uom_not_configured" or
            "storefront_checkout.sales_channel_not_configured" => Conflict(response),
            _ => BadRequest(response)
        };
    }

    private bool TryGetCustomerContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    private IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "storefront_checkout.invalid_customer_context",
            "A valid customer session is required.")));

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
