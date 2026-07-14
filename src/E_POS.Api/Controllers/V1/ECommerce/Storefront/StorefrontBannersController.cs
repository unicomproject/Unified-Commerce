using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Storefront;

[ApiController]
[Route("api/v1/ecommerce/storefront/banners")]
public class StorefrontBannersController : ControllerBase
{
    private readonly IStorefrontBannerService _storefrontBannerService;

    public StorefrontBannersController(IStorefrontBannerService storefrontBannerService)
    {
        _storefrontBannerService = storefrontBannerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StorefrontBannerReadModel>>> GetBanners(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromQuery] string bannerType,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new { Error = "X-Tenant-Id header is required" });
        }

        if (string.IsNullOrWhiteSpace(bannerType))
        {
            return BadRequest(new { Error = "BannerType is required" });
        }

        var banners = await _storefrontBannerService.GetActiveBannersAsync(tenantId, bannerType, cancellationToken);
        return Ok(banners);
    }
}