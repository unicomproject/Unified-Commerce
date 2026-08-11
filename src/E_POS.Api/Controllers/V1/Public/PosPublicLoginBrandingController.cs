using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/pos/public/login-branding")]
public sealed class PosPublicLoginBrandingController : ControllerBase
{
    private readonly IPosLoginBrandingService _service;

    public PosPublicLoginBrandingController(IPosLoginBrandingService service) => _service = service;

    [HttpGet("{tenantSlug}")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(PublicPosLoginBrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string tenantSlug, CancellationToken cancellationToken)
    {
        var result = await _service.GetPublicAsync(tenantSlug, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return result.Error.Code == "pos_login_branding.invalid_slug"
                ? BadRequest(Error(result.Error))
                : NotFound(Error(result.Error));

        var etag = $"\"{result.Value.UpdatedAt.UtcTicks:x}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public,max-age=300";
        if (Request.Headers.IfNoneMatch.Any(x => string.Equals(x, etag, StringComparison.Ordinal)))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(result.Value);
    }

    private object Error(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
