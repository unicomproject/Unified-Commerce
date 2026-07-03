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
[Route("api/v1/platform-admin/users")]
public sealed class PlatformAdminUsersController : ControllerBase
{
    private readonly IPlatformUserService _userService;

    public PlatformAdminUsersController(IPlatformUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformUserListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _userService.GetUsersAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Platform users loaded successfully.");
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformUserDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _userService.GetUserAsync(userId, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform user loaded successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformUserDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreatePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _userService.CreateUserAsync(request, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform user created successfully.");
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformUserDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(
        Guid userId,
        [FromBody] UpdatePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _userService.UpdateUserAsync(userId, request, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform user updated successfully.");
    }

    [HttpPut("{userId:guid}/roles")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformUserDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRoles(
        Guid userId,
        [FromBody] AssignPlatformUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _userService.AssignRolesAsync(userId, request, platformUserId, cancellationToken);
        return ToActionResult(result, "Platform user roles updated successfully.");
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
            "platform_users.not_found" => NotFound(CreateLegacyError(error.Code, error.Message)),
            "platform_users.validation_failed" => BadRequest(CreateLegacyError(error.Code, error.Message)),
            "platform_users.conflict" or "platform_users.super_admin_lockout" =>
                StatusCode(StatusCodes.Status409Conflict, CreateLegacyError(error.Code, error.Message)),
            "platform_users.protected_role_denied" =>
                StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(error.Code, error.Message)),
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
