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
[Route("api/v1/platform-admin/catalog")]
public sealed class PlatformAdminCatalogController : ControllerBase
{
    private readonly IPlatformModulesCatalogService _modulesCatalogService;

    public PlatformAdminCatalogController(IPlatformModulesCatalogService modulesCatalogService)
    {
        _modulesCatalogService = modulesCatalogService;
    }

    [HttpGet("modules")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformModulesCatalogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetModules(
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        if (!IsValidScope(scope))
        {
            return BadRequest(CreateLegacyError(
                "platform_modules_catalog.invalid_scope",
                "Invalid scope parameter. Allowed values are 'all', 'platform', or 'tenant'."));
        }

        var result = await _modulesCatalogService.GetModulesAsync(platformUserId, scope, cancellationToken);
        return ToActionResult(result, "Platform modules catalog loaded successfully.");
    }

    private static bool IsValidScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return true;
        }

        var normalized = scope.Trim().ToLowerInvariant();
        return normalized is "all" or "platform" or "tenant";
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


