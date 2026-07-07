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
[Route("api/v1/platform-admin/roles")]
public sealed class PlatformAdminRolesController : ControllerBase
{
    private readonly IPlatformRoleService _roleService;

    public PlatformAdminRolesController(IPlatformRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformRoleListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _roleService.GetRolesAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform roles loaded successfully.");
    }

    [HttpGet("{roleId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformRoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _roleService.GetRoleAsync(roleId, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform role loaded successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformRoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreatePlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _roleService.CreateRoleAsync(request, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform role created successfully.");
    }

    [HttpPut("{roleId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformRoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRole(
        Guid roleId,
        [FromBody] UpdatePlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _roleService.UpdateRoleAsync(roleId, request, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform role updated successfully.");
    }

    [HttpGet("{roleId:guid}/permissions")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformRolePermissionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRolePermissions(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _roleService.GetRolePermissionsAsync(roleId, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform role permissions loaded successfully.");
    }

    [HttpPut("{roleId:guid}/permissions")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformRolePermissionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId,
        [FromBody] UpdatePlatformRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _roleService.UpdateRolePermissionsAsync(
            roleId,
            request,
            platformUserId,
            cancellationToken);

        return ToActionResult(result, "Platform role permissions updated successfully.");
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
            "platform_roles.not_found" => NotFound(CreateLegacyError(error.Code, error.Message)),
            "platform_roles.validation_failed" => BadRequest(CreateLegacyError(error.Code, error.Message)),
            "platform_roles.conflict" or "platform_roles.system_role_protected" =>
                StatusCode(StatusCodes.Status409Conflict, CreateLegacyError(error.Code, error.Message)),
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


