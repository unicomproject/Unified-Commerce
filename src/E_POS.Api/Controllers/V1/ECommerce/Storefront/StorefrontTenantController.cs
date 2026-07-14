using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/tenant")]
public class StorefrontTenantController : ControllerBase
{
    private readonly IStorefrontTenantService _storefrontTenantService;

    public StorefrontTenantController(IStorefrontTenantService storefrontTenantService)
    {
        _storefrontTenantService = storefrontTenantService;
    }

    [HttpGet("resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveTenant([FromQuery] string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(new { message = "Tenant slug is required." });
        }

        var tenantId = await _storefrontTenantService.ResolveTenantIdAsync(slug, cancellationToken);

        if (tenantId == null)
        {
            return NotFound(new { message = "Tenant not found or inactive." });
        }

        return Ok(new { tenantId });
    }
}