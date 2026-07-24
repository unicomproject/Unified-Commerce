using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace E_POS.Api.Controllers.V1.ECommerce.CustomerAuth;

[ApiController]
[Route("api/v1/ecommerce/storefront/auth")]
public sealed class CustomerAuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "customer_refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/ecommerce/storefront/auth";
    private readonly ICustomerAuthService _service;

    public CustomerAuthController(ICustomerAuthService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> Login(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.LoginAsync(
            tenantId,
            request,
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            AppendRefreshTokenCookie(result.Value);
            return Ok(new
            {
                success = true,
                message = "Login successful.",
                data = result.Value.Response
            });
        }

        var error = CreateError(result.Error);
        return result.Error.Code switch
        {
            "customer_auth.validation_failed" => BadRequest(error),
            "customer_auth.tenant_access_denied" => StatusCode(StatusCodes.Status403Forbidden, error),
            _ => Unauthorized(error)
        };
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> Refresh(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var result = await _service.RefreshAsync(
            tenantId,
            refreshToken ?? string.Empty,
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            AppendRefreshTokenCookie(result.Value);
            return Ok(new
            {
                success = true,
                message = "Token refreshed.",
                data = result.Value.Response
            });
        }

        ClearRefreshTokenCookie();
        return Unauthorized(CreateError(result.Error));
    }

    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!TryGetSessionContext(out var tenantId, out var customerId, out var sessionId))
            return Unauthorized(CreateError(
                new ApplicationError("customer_auth.invalid_session", "Invalid customer session.")));

        var result = await _service.LogoutAsync(
            tenantId, customerId, sessionId, cancellationToken);
        if (result.IsFailure)
            return Unauthorized(CreateError(result.Error));

        ClearRefreshTokenCookie();
        return NoContent();
    }

    private bool TryGetSessionContext(
        out Guid tenantId,
        out Guid customerId,
        out Guid sessionId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        sessionId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId) &&
               Guid.TryParse(User.FindFirstValue("session_id"), out sessionId);
    }

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };

    private void AppendRefreshTokenCookie(CustomerAuthTokenResult result)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            result.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = ShouldUseSecureRefreshCookie(),
                SameSite = SameSiteMode.Strict,
                Expires = result.RefreshTokenExpiresAt,
                Path = RefreshTokenCookiePath
            });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = ShouldUseSecureRefreshCookie(),
                SameSite = SameSiteMode.Strict,
                Path = RefreshTokenCookiePath
            });
    }

    private bool ShouldUseSecureRefreshCookie()
    {
        if (Request.IsHttps)
            return true;

        var services = HttpContext.RequestServices;
        if (services is null)
            return true;

        var environment = services.GetService<IWebHostEnvironment>();
        return environment is null || !environment.IsDevelopment();
    }
}
