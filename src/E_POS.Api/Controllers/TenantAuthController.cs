using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Application.Modules.AuthSecurity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
// Tenant login stays anonymous because authentication starts at this endpoint.
[AllowAnonymous]
[Route("api/v1/tenant-auth")]
public sealed class TenantAuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "tenant_refresh_token";
    private readonly ITenantAuthService _tenantAuthService;

    public TenantAuthController(ITenantAuthService tenantAuthService)
    {
        _tenantAuthService = tenantAuthService;
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(TenantLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] TenantLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tenantAuthService.LoginAsync(request, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            AppendRefreshTokenCookie(result.Value);
            return Ok(result.Value);
        }

        return result.Error.Code switch
        {
            "tenant_auth.validation_failed" => BadRequest(CreateError(result.Error)),
            "tenant_auth.tenant_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(result.Error)),
            _ => Unauthorized(CreateError(result.Error))
        };
    }

    private void AppendRefreshTokenCookie(TenantLoginResponse response)
    {
        // Browser clients use the HttpOnly cookie; native POS/mobile clients use secure app storage.
        Response.Cookies.Append(RefreshTokenCookieName, response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = response.RefreshTokenExpiresAt,
            Path = "/api/v1/tenant-auth"
        });
    }

    private object CreateError(ApplicationError error)
    {
        return new
        {
            code = error.Code,
            message = error.Message,
            details = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };
    }
}