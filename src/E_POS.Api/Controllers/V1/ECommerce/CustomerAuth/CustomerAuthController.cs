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
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> Register(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerRegisterRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(
            tenantId,
            request ?? new CustomerRegisterRequest(),
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);

        return result.IsSuccess
            ? Ok(CreateSuccess("Registration successful. Please check your email for the verification code."))
            : CreateErrorResult(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> VerifyEmail(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerVerifyEmailRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.VerifyEmailAsync(
            tenantId,
            request ?? new CustomerVerifyEmailRequest(),
            cancellationToken);

        return result.IsSuccess
            ? Ok(CreateSuccess("Email verified successfully."))
            : CreateErrorResult(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("resend-email-verification")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> ResendEmailVerification(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerResendEmailVerificationRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ResendEmailVerificationAsync(
            tenantId,
            request ?? new CustomerResendEmailVerificationRequest(),
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);

        return result.IsSuccess
            ? Ok(CreateSuccess("Verification code sent."))
            : CreateErrorResult(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> ForgotPassword(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerForgotPasswordRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ForgotPasswordAsync(
            tenantId,
            request ?? new CustomerForgotPasswordRequest(),
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.FirstOrDefault(),
            cancellationToken);

        return result.IsSuccess
            ? Ok(CreateSuccess("If an account exists for this email, a password reset link has been sent."))
            : CreateErrorResult(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    public async Task<IActionResult> ResetPassword(
        [FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
        [FromBody] CustomerResetPasswordRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ResetPasswordAsync(
            tenantId,
            request ?? new CustomerResetPasswordRequest(),
            cancellationToken);

        return result.IsSuccess
            ? Ok(CreateSuccess("Password reset successful."))
            : CreateErrorResult(result.Error);
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
            "customer_auth.email_not_verified" => StatusCode(StatusCodes.Status403Forbidden, error),
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