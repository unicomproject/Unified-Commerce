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

        var result = await _storefrontTenantService.ResolveTenantAsync(slug, cancellationToken);

        if (result.TenantId == null)
        {
            return NotFound(new { message = "Tenant not found or inactive." });
        }

        return Ok(new { tenantId = result.TenantId, currencyCode = result.BaseCurrencyCode });
    }
}