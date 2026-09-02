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
    public async Task<IActionResult> ResolveTenant(
        [FromQuery] string? slug = null,
        CancellationToken cancellationToken = default,
        [FromQuery] string? host = null)
    {
        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(host))
        {
            return BadRequest(new { message = "Tenant slug or host is required." });
        }

        var result = !string.IsNullOrWhiteSpace(host)
            ? await _storefrontTenantService.ResolveTenantByHostAsync(NormalizeHost(host), cancellationToken)
            : await _storefrontTenantService.ResolveTenantAsync(slug!, cancellationToken);

        if (result.TenantId == null)
        {
            return NotFound(new { message = "Tenant not found or inactive." });
        }

        return Ok(new { tenantId = result.TenantId, currencyCode = result.BaseCurrencyCode, storeName = result.StoreName, logoUrl = result.LogoUrl });
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim();
        if (Uri.TryCreate(
                normalized.Contains("://", StringComparison.Ordinal) ? normalized : $"https://{normalized}",
                UriKind.Absolute,
                out var uri))
        {
            return uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        }

        return normalized.Split(':', 2)[0].TrimEnd('.').ToLowerInvariant();
    }
}
