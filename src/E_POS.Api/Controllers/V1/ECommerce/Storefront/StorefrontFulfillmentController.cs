using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/fulfillment")]
public class StorefrontFulfillmentController : ControllerBase
{
    private readonly IStorefrontFulfillmentService _storefrontFulfillmentService;

    public StorefrontFulfillmentController(IStorefrontFulfillmentService storefrontFulfillmentService)
    {
        _storefrontFulfillmentService = storefrontFulfillmentService;
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores([FromHeader(Name = "X-Tenant-Id")] Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new { Error = "X-Tenant-Id header is required" });
        }

        var stores = await _storefrontFulfillmentService.GetAvailableStoresAsync(tenantId, cancellationToken);
        return Ok(stores);
    }

    [HttpGet("stores/{outletId:guid}/collection-options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCollectionOptions(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromRoute] Guid outletId,
        [FromQuery] int days = 5,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            return BadRequest(new { Error = "X-Tenant-Id header is required" });
        if (outletId == Guid.Empty)
            return BadRequest(new { Error = "A valid outlet id is required" });

        var result = await _storefrontFulfillmentService.GetCollectionOptionsAsync(
            tenantId,
            outletId,
            days,
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return Ok(result.Value);

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
            "storefront_fulfillment.collection_unavailable" => NotFound(error),
            "storefront_fulfillment.collection_configuration_missing" or
            "storefront_fulfillment.invalid_timezone" => Conflict(error),
            _ => BadRequest(error)
        };
    }
}
