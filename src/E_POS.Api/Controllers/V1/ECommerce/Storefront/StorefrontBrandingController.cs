using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/branding")]
public sealed class StorefrontBrandingController : ControllerBase
{
    private readonly IStorefrontBrandingService _storefrontBrandingService;

    public StorefrontBrandingController(IStorefrontBrandingService storefrontBrandingService)
    {
        _storefrontBrandingService = storefrontBrandingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StorefrontBrandingReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranding(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new { error = "X-Tenant-Id header is required" });
        }

        var branding = await _storefrontBrandingService.GetBrandingAsync(tenantId, cancellationToken);
        return branding is null
            ? NotFound(new { error = "Storefront tenant was not found or is inactive" })
            : Ok(branding);
    }
}
