using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace E_POS.Api.Controllers.V1.Tenant.TenantAuth;

/// <summary>
/// Public Tenant Admin invitation validate / set-password endpoints.
/// Routes match Flutter AuthRemoteDatasource contracts.
/// </summary>
[ApiController]
[Route("api/tenant-admin/onboarding")]
[AllowAnonymous]
public sealed class TenantAdminOnboardingInvitationController : ControllerBase
{
    private readonly ITenantAdminInvitationAcceptanceService _acceptanceService;

    public TenantAdminOnboardingInvitationController(ITenantAdminInvitationAcceptanceService acceptanceService)
    {
        _acceptanceService = acceptanceService;
    }

    [HttpGet("setup-token/{token}/validate")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(ValidateTenantAdminSetupTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ValidateSetupToken(string token, CancellationToken cancellationToken)
    {
        var response = await _acceptanceService.ValidateSetupTokenAsync(token, cancellationToken);
        // Flat camelCase JSON — Flutter SetupTokenValidationDto maps response.data as the root.
        return Ok(new
        {
            setupToken = response.SetupToken,
            valid = response.Valid,
            expired = response.Expired,
            email = response.Email,
            message = response.Message
        });
    }

    [HttpPost("setup-password")]
    [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
    [ProducesResponseType(typeof(SetupTenantAdminPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SetupPassword(
        [FromBody] SetupTenantAdminPasswordRequestBody? body,
        CancellationToken cancellationToken)
    {
        var request = new SetupTenantAdminPasswordRequest(
            body?.SetupToken ?? string.Empty,
            body?.Password ?? string.Empty,
            body?.ConfirmPassword ?? string.Empty);

        var result = await _acceptanceService.SetupPasswordAsync(request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new
            {
                success = result.Value.Success,
                message = result.Value.Message
            });
        }

        return BadRequest(CreateError(result.Error));
    }

    private object CreateError(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };

    public sealed class SetupTenantAdminPasswordRequestBody
    {
        public string? SetupToken { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
