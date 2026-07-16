using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/cart")]
public sealed class StorefrontCartController : ControllerBase
{
    private readonly IStorefrontCartService _service;

    public StorefrontCartController(IStorefrontCartService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(TenantId(), SessionId(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddStorefrontCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AddItemAsync(TenantId(), SessionId(), request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(
        [FromRoute] Guid itemId,
        [FromBody] UpdateStorefrontCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateItemAsync(
            TenantId(), SessionId(), itemId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _service.RemoveItemAsync(
            TenantId(), SessionId(), itemId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var result = await _service.ClearAsync(TenantId(), SessionId(), cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(ApplicationResult<StorefrontCartReadModel> result)
    {
        if (result.IsSuccess && result.Value is not null)
            return Ok(new { success = true, message = "Cart updated successfully.", data = result.Value });

        var error = new
        {
            success = false,
            message = result.Error.Message,
            errorCode = result.Error.Code,
            errors = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier
        };
        return result.Error.Code switch
        {
            "storefront_cart.product_not_found" or
            "storefront_cart.variant_not_found" or
            "storefront_cart.item_not_found" => NotFound(error),
            "storefront_cart.insufficient_stock" or
            "storefront_cart.expired" => Conflict(error),
            _ => BadRequest(error)
        };
    }

    private Guid TenantId() =>
        Guid.TryParse(Request.Headers["X-Tenant-Id"].FirstOrDefault(), out var tenantId)
            ? tenantId
            : Guid.Empty;

    private string? SessionId() => Request.Headers["X-Cart-Session-Id"].FirstOrDefault();
}
