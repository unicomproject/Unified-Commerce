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
}