using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform-admin/tenants")]
public sealed class PlatformAdminTenantsController : ControllerBase
{
    private readonly IPlatformTenantService _tenantService;

    public PlatformAdminTenantsController(IPlatformTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.GetSummaryAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform tenant summary loaded successfully.");
    }

    [HttpGet("filter-options")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantFilterOptionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.GetFilterOptionsAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform tenant filter options loaded successfully.");
    }

    [HttpGet("create-options")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantCreateOptionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCreateOptions(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.GetCreateOptionsAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform tenant create options loaded successfully.");
    }

    [HttpGet]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] PlatformTenantListQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.GetTenantsAsync(query, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform tenants loaded successfully.");
    }

    [HttpGet("{tenantId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantById(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.GetTenantDetailAsync(tenantId, platformUserId, cancellationToken);
        return ToDetailActionResult(result, "Platform tenant loaded successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantDetailResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.CreateTenantAsync(request, platformUserId, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                LegacyApiResponse<PlatformTenantDetailResponse>.Ok(
                    "Platform tenant created successfully.",
                    result.Value));
        }

        return MapError(result.Error);
    }

    [HttpPut("{tenantId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(
        Guid tenantId,
        [FromBody] UpdatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.UpdateTenantAsync(tenantId, request, platformUserId, cancellationToken);
        return ToDetailActionResult(result, "Platform tenant updated successfully.");
    }

    [HttpPost("{tenantId:guid}/activate")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.ActivateTenantAsync(tenantId, platformUserId, cancellationToken);
        return ToDetailActionResult(result, "Platform tenant activated successfully.");
    }

    [HttpPost("{tenantId:guid}/suspend")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SuspendTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.SuspendTenantAsync(tenantId, platformUserId, cancellationToken);
        return ToDetailActionResult(result, "Platform tenant suspended successfully.");
    }

    [HttpPut("{tenantId:guid}/entitlements")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntitlements(
        Guid tenantId,
        [FromBody] UpdatePlatformTenantEntitlementsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError(
                "platform_auth.invalid_session",
                "Invalid platform session.")));
        }

        var result = await _tenantService.UpdateEntitlementsAsync(tenantId, request, platformUserId, cancellationToken);
        return ToDetailActionResult(result, "Platform tenant entitlements updated successfully.");
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<T>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult ToDetailActionResult(
        ApplicationResult<PlatformTenantDetailResponse> result,
        string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<PlatformTenantDetailResponse>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult MapError(ApplicationError error)
    {
        return error.Code switch
        {
            "platform_tenants.not_found" => NotFound(CreateLegacyError(error)),
            "platform_tenants.validation_failed" => BadRequest(CreateLegacyError(error)),
            "platform_tenants.conflict" or "platform_tenants.invalid_transition" =>
                StatusCode(StatusCodes.Status409Conflict, CreateLegacyError(error)),
            _ => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(error))
        };
    }

    private bool TryGetPlatformUserId(out Guid platformUserId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(platformUserIdValue, out platformUserId);
    }

    private object CreateLegacyError(ApplicationError error)
    {
        var fieldErrors = error.FieldErrors?
            .Select(item => new { field = item.Field, message = item.Message })
            .ToArray<object>() ?? Array.Empty<object>();

        return new
        {
            success = false,
            message = error.Message,
            errorCode = error.Code,
            errors = fieldErrors,
            traceId = HttpContext.TraceIdentifier
        };
    }
}
