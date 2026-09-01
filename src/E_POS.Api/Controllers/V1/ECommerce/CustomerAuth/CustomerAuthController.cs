using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
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
public sealed class CustomerAuthController : CustomerControllerBase
{
    private const string RefreshTokenCookieName = "customer_refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/ecommerce/storefront/auth";
    private readonly ICustomerAuthService _service;

    public CustomerAuthController(ICustomerAuthService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpPost("request-otp")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> RequestOtp(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerRequestOtpRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RequestOtpAsync(
            tenantId,
            request ?? new CustomerRequestOtpRequest(),
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);

        return result.IsSuccess
            ? Ok(CreateSuccess("If the email is valid, an OTP has been sent."))
            : CreateErrorResult(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> VerifyOtp(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerVerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.VerifyOtpAsync(
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
                message = "Authentication successful.",
                data = result.Value.Response
            });
        }

        var error = CreateError(result.Error);
        return result.Error.Code switch
        {
            "customer_auth.validation_failed" => BadRequest(error),
            "customer_auth.tenant_access_denied" => StatusCode(StatusCodes.Status403Forbidden, error),
            "customer_auth.email_not_verified" => StatusCode(StatusCodes.Status403Forbidden, error),
            "customer_auth.invalid_verification_code" => Unauthorized(error),
            _ => Unauthorized(error)
        };
    }

    [AllowAnonymous]
    [HttpPost("google")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> GoogleLogin(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerGoogleLoginRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.GoogleLoginAsync(
            tenantId,
            request ?? new CustomerGoogleLoginRequest(),
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
            "customer_auth.terms_required" => BadRequest(error),
            "customer_auth.tenant_access_denied" => StatusCode(StatusCodes.Status403Forbidden, error),
            "customer_auth.google_email_not_verified" => StatusCode(StatusCodes.Status403Forbidden, error),
            "customer_auth.invalid_google_token" => Unauthorized(error),
            "customer_auth.email_already_registered" => Conflict(error),
            "customer_auth.external_account_conflict" => Conflict(error),
            "customer_auth.google_not_configured" => StatusCode(StatusCodes.Status500InternalServerError, error),
            "customer_auth.google_verification_unavailable" => StatusCode(StatusCodes.Status504GatewayTimeout, error),
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

    private IActionResult CreateErrorResult(ApplicationError error)
    {
        var body = CreateError(error);
        return error.Code switch
        {
            "customer_auth.validation_failed" => BadRequest(body),
            "customer_auth.terms_required" => BadRequest(body),
            "customer_auth.invalid_verification_code" => BadRequest(body),
            "customer_auth.invalid_reset_token" => BadRequest(body),
            "customer_auth.invalid_google_token" => Unauthorized(body),
            "customer_auth.google_email_not_verified" => StatusCode(StatusCodes.Status403Forbidden, body),
            "customer_auth.external_account_conflict" => Conflict(body),
            "customer_auth.google_not_configured" => StatusCode(StatusCodes.Status500InternalServerError, body),
            "customer_auth.google_verification_unavailable" => StatusCode(StatusCodes.Status504GatewayTimeout, body),
            "customer_auth.email_already_registered" => Conflict(body),
            "customer_auth.tenant_access_denied" => StatusCode(StatusCodes.Status403Forbidden, body),
            "customer_auth.email_not_verified" => StatusCode(StatusCodes.Status403Forbidden, body),
            "customer_auth.email_delivery_unavailable" => StatusCode(StatusCodes.Status500InternalServerError, body),
            _ => BadRequest(body)
        };
    }

    private object CreateSuccess(string message) => new
    {
        success = true,
        message
    };

    private void AppendRefreshTokenCookie(CustomerAuthTokenResult result)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureRefreshCookie(),
            SameSite = SameSiteMode.Strict,
            Path = RefreshTokenCookiePath
        };

        if (result.RememberMe)
        {
            options.Expires = result.RefreshTokenExpiresAt;
        }

        Response.Cookies.Append(
            RefreshTokenCookieName,
            result.RefreshToken,
            options);
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