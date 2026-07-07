using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform-admin/settings")]
public sealed class PlatformAdminSettingsController : ControllerBase
{
    private readonly IPlatformSettingsService _settingsService;

    public PlatformAdminSettingsController(IPlatformSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _settingsService.GetSettingsAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform settings loaded successfully.");
    }

    [HttpPut]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _settingsService.UpdateSettingsAsync(request, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform settings updated successfully.");
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<T>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult MapError(ApplicationError error)
    {
        return error.Code switch
        {
            "platform_settings.validation_failed" => BadRequest(CreateLegacyError(error.Code, error.Message)),
            _ => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(error.Code, error.Message))
        };
    }

    private bool TryGetPlatformUserId(out Guid platformUserId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(platformUserIdValue, out platformUserId);
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


