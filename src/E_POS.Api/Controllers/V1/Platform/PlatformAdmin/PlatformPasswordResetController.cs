using E_POS.Api.Common.Auth;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers;

[ApiController]
[Route("api/v1/platform-auth/password-reset")]
public sealed class PlatformPasswordResetController : ControllerBase
{
    private readonly IPlatformPasswordResetService _passwordResetService;

    public PlatformPasswordResetController(IPlatformPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [AllowAnonymous]
    [HttpPost("validate")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(ValidatePlatformPasswordResetTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Validate(
        [FromBody] ValidatePlatformPasswordResetTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _passwordResetService.ValidatePublicTokenAsync(
            request?.Token ?? string.Empty,
            cancellationToken);

        return Ok(result.Value ?? new ValidatePlatformPasswordResetTokenResponse(false, "INVALID", null));
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(CompletePlatformPasswordResetResponse), StatusCodes.Status200OK)]
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
            return Ok(result.Value);
        }

        return BadRequest(CreateError(result.Error));
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
