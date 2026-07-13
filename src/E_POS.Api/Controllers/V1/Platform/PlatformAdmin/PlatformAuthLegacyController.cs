using System.Security.Claims;
using E_POS.Api.Common.Auth;
using E_POS.Api.Common.Cookies;
using E_POS.Api.Extensions;
using E_POS.Api.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class PlatformAuthLegacyController : ControllerBase
{
    private readonly IPlatformAuthService _platformAuthService;

    public PlatformAuthLegacyController(IPlatformAuthService platformAuthService)
    {
        _platformAuthService = platformAuthService;
    }

    [AllowAnonymous]
    [HttpPost("platform-login")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(LegacyApiResponse<LegacyPlatformLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PlatformLogin(
        [FromBody] PlatformAdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _platformAuthService.LoginAsync(
            request,
            cancellationToken,
            PlatformAuthClientContextFactory.FromHttpContext(HttpContext));

        if (result.IsSuccess && result.Value is not null)
        {
            PlatformAuthCookieHelper.AppendRefreshTokenCookie(
                Response,
                result.Value,
                PlatformAuthCookieHelper.LegacyAuthCookiePath);

            return Ok(LegacyApiResponse<LegacyPlatformLoginResponse>.Ok(
                "Platform login successful.",
                PlatformAuthCookieHelper.ToLegacyResponse(result.Value)));
        }

        return result.Error.Code switch
        {
            "platform_auth.validation_failed" => BadRequest(CreateLegacyError(result.Error.Code, result.Error.Message)),
            "platform_auth.platform_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(result.Error.Code, result.Error.Message)),
            _ => Unauthorized(CreateLegacyError(result.Error.Code, result.Error.Message))
        };
    }

    [AllowAnonymous]
    [HttpPost("platform-refresh")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(LegacyApiResponse<LegacyPlatformLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PlatformRefresh(CancellationToken cancellationToken)
    {
        var result = await _platformAuthService.RefreshAsync(
            Request.Cookies[PlatformAuthCookieHelper.RefreshTokenCookieName] ?? string.Empty,
            cancellationToken,
            PlatformAuthClientContextFactory.FromHttpContext(HttpContext));

        if (result.IsSuccess && result.Value is not null)
        {
            PlatformAuthCookieHelper.AppendRefreshTokenCookie(
                Response,
                result.Value,
                PlatformAuthCookieHelper.LegacyAuthCookiePath);

            return Ok(LegacyApiResponse<LegacyPlatformLoginResponse>.Ok(
                "Platform session refreshed.",
                PlatformAuthCookieHelper.ToLegacyResponse(result.Value)));
        }

        PlatformAuthCookieHelper.ClearRefreshTokenCookie(Response, PlatformAuthCookieHelper.LegacyAuthCookiePath);

        return result.Error.Code switch
        {
            "platform_auth.platform_access_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(result.Error.Code, result.Error.Message)),
            _ => Unauthorized(CreateLegacyError(result.Error.Code, result.Error.Message))
        };
    }

    [AllowAnonymous]
    [HttpPost("platform-logout")]
    [ProducesResponseType(typeof(LegacyApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PlatformLogout(CancellationToken cancellationToken)
    {
        if (TryGetPlatformSessionContext(out var platformUserId, out var sessionId))
        {
            await _platformAuthService.LogoutAsync(platformUserId, sessionId, cancellationToken);
        }
        else
        {
            await _platformAuthService.LogoutByRefreshTokenAsync(
                Request.Cookies[PlatformAuthCookieHelper.RefreshTokenCookieName] ?? string.Empty,
                cancellationToken);
        }

        PlatformAuthCookieHelper.ClearRefreshTokenCookie(Response, PlatformAuthCookieHelper.LegacyAuthCookiePath);

        return Ok(LegacyApiResponse<bool>.Ok("Logout successful.", true));
    }

    private bool TryGetPlatformSessionContext(out Guid platformUserId, out Guid sessionId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdValue = User.FindFirstValue("session_id");

        var hasPlatformUserId = Guid.TryParse(platformUserIdValue, out platformUserId);
        var hasSessionId = Guid.TryParse(sessionIdValue, out sessionId);

        return hasPlatformUserId && hasSessionId;
    }

    private object CreateLegacyError(string errorCode, string message)
    {
        return new
        {
            success = false,
            message,
            errorCode,
            errors = Array.Empty<object>(),
            traceId = HttpContext.TraceIdentifier
        };
    }
}


