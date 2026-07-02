using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
[Route("api/v1/platform-auth")]
public sealed class PlatformAuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "platform_refresh_token";
    private readonly IPlatformAuthService _platformAuthService;

    public PlatformAuthController(IPlatformAuthService platformAuthService)
    {
        _platformAuthService = platformAuthService;
    }

    // Platform login stays anonymous because authentication starts at this endpoint.
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(PlatformAdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] PlatformAdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _platformAuthService.LoginAsync(request, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            AppendRefreshTokenCookie(result.Value);
            return Ok(result.Value);
        }

        return result.Error.Code switch
        {
            "platform_auth.validation_failed" => BadRequest(CreateError(result.Error)),
            "platform_auth.platform_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(result.Error)),
            _ => Unauthorized(CreateError(result.Error))
        };
    }

    [Authorize(Policy = "PlatformOnly")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformSessionContext(out var platformUserId, out var sessionId))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _platformAuthService.LogoutAsync(platformUserId, sessionId, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(CreateError(result.Error));
        }

        ClearRefreshTokenCookie();
        return NoContent();
    }

    private void AppendRefreshTokenCookie(PlatformAdminLoginResponse response)
    {
        // Keep the refresh token out of JavaScript-readable storage.
        Response.Cookies.Append(RefreshTokenCookieName, response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = response.RefreshTokenExpiresAt,
            Path = "/api/v1/platform-auth"
        });
    }

    private void ClearRefreshTokenCookie()
    {
        // Clearing the HttpOnly cookie removes the browser-held refresh token after server revoke.
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/platform-auth"
        });
    }

    private bool TryGetPlatformSessionContext(out Guid platformUserId, out Guid sessionId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdValue = User.FindFirstValue("session_id");

        var hasPlatformUserId = Guid.TryParse(platformUserIdValue, out platformUserId);
        var hasSessionId = Guid.TryParse(sessionIdValue, out sessionId);

        return hasPlatformUserId && hasSessionId;
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