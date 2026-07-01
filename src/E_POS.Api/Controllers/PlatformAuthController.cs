using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
// Platform login stays anonymous because authentication starts at this endpoint.
[AllowAnonymous]
[Route("api/v1/platform-auth")]
public sealed class PlatformAuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "platform_refresh_token";
    private readonly IPlatformAuthService _platformAuthService;

    public PlatformAuthController(IPlatformAuthService platformAuthService)
    {
        _platformAuthService = platformAuthService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(PlatformAdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

