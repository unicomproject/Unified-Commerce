using E_POS.Api.Common.Auth;
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
public sealed class PlatformPasswordResetLegacyController : ControllerBase
{
    private readonly IPlatformPasswordResetService _passwordResetService;

    public PlatformPasswordResetLegacyController(IPlatformPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [AllowAnonymous]
    [HttpPost("platform-password-reset/validate")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(LegacyApiResponse<ValidatePlatformPasswordResetTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Validate(
        [FromBody] ValidatePlatformPasswordResetTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _passwordResetService.ValidatePublicTokenAsync(
            request?.Token ?? string.Empty,
            cancellationToken);

        var payload = result.Value ?? new ValidatePlatformPasswordResetTokenResponse(false, "INVALID", null);
        return Ok(LegacyApiResponse<ValidatePlatformPasswordResetTokenResponse>.Ok(
            "Password reset token validated.",
            payload));
    }

    [AllowAnonymous]
    [HttpPost("platform-password-reset/complete")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(LegacyApiResponse<CompletePlatformPasswordResetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Complete(
        [FromBody] CompletePlatformPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _passwordResetService.CompletePasswordResetAsync(
            request,
            PlatformAuthClientContextFactory.FromHttpContext(HttpContext),
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<CompletePlatformPasswordResetResponse>.Ok(
                result.Value.Message,
                result.Value));
        }

        return BadRequest(new
        {
            success = false,
            message = result.Error.Message,
            errorCode = result.Error.Code,
            errors = Array.Empty<object>(),
            traceId = HttpContext.TraceIdentifier
        });
    }
}
