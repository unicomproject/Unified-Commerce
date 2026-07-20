using System.Security.Claims;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.CustomerWishlist;

[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v1/ecommerce/storefront/wishlist")]
public sealed class CustomerWishlistController : ControllerBase
{
    private readonly ICustomerWishlistService _service;

    public CustomerWishlistController(ICustomerWishlistService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.GetAsync(tenantId, customerId, cancellationToken),
            "Wishlist retrieved successfully.");
    }

    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddCustomerWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.AddItemAsync(
                tenantId,
                customerId,
                request,
                cancellationToken),
            "Wishlist updated successfully.");
    }

    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.RemoveItemAsync(
                tenantId,
                customerId,
                itemId,
                cancellationToken),
            "Wishlist item removed successfully.");
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        return ToActionResult(
            await _service.ClearAsync(tenantId, customerId, cancellationToken),
            "Wishlist cleared successfully.");
    }

    private IActionResult ToActionResult(
        ApplicationResult<CustomerWishlistReadModel> result,
        string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { success = true, message = successMessage, data = result.Value });

        var error = CreateError(result.Error);
        return result.Error.Code switch
        {
            "customer_wishlist.customer_not_found" or
            "customer_wishlist.product_not_found" or
            "customer_wishlist.variant_not_found" or
            "customer_wishlist.item_not_found" => NotFound(error),
            "customer_wishlist.invalid_customer_context" => Unauthorized(error),
            _ => BadRequest(error)
        };
    }

    private bool TryGetCustomerContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    private IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "customer_wishlist.invalid_customer_context",
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
