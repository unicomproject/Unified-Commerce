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
[Route("api/v1/platform-admin/business-capability-map")]
public sealed class PlatformAdminBusinessCapabilityController : ControllerBase
{
    private readonly IBusinessCapabilityCatalogService _capabilityCatalogService;

    public PlatformAdminBusinessCapabilityController(IBusinessCapabilityCatalogService capabilityCatalogService)
    {
        _capabilityCatalogService = capabilityCatalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LegacyApiResponse<BusinessCapabilityMapResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBusinessCapabilityMap(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _capabilityCatalogService.GetBusinessCapabilityMapAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform business capability map loaded successfully.");
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<T>.Ok(successMessage, result.Value));
        }

        return StatusCode(
            StatusCodes.Status403Forbidden,
            CreateLegacyError(result.Error.Code, result.Error.Message));
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
