using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
[Route("api/v1/tenant-auth")]
public sealed class TenantAuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "tenant_refresh_token";
    private readonly ITenantAuthService _tenantAuthService;

    public TenantAuthController(ITenantAuthService tenantAuthService)
    {
        _tenantAuthService = tenantAuthService;
    }

    // Tenant login stays anonymous because authentication starts at this endpoint.
    [AllowAnonymous]
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

    [Authorize(Policy = "TenantOnly")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!TryGetTenantSessionContext(out var tenantUserId, out var tenantId, out var sessionId))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "tenant_auth.invalid_session",
                "Invalid tenant session.")));
        }

        var result = await _tenantAuthService.LogoutAsync(tenantUserId, tenantId, sessionId, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(CreateError(result.Error));
        }

        ClearRefreshTokenCookie();
        return NoContent();
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

    private void ClearRefreshTokenCookie()
    {
        // Clearing the HttpOnly cookie completes browser logout; native clients clear their secure store separately.
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/tenant-auth"
        });
    }

    private bool TryGetTenantSessionContext(out Guid tenantUserId, out Guid tenantId, out Guid sessionId)
    {
        var tenantUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = User.FindFirstValue("tenant_id");
        var sessionIdValue = User.FindFirstValue("session_id");

        var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out tenantUserId);
        var hasTenantId = Guid.TryParse(tenantIdValue, out tenantId);
        var hasSessionId = Guid.TryParse(sessionIdValue, out sessionId);

        return hasTenantUserId && hasTenantId && hasSessionId;
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

