using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.AccessControl;

[ApiController]
[Route("api/v1/tenant-admin")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminRolesController : ControllerBase
{
    private readonly ITenantAdminRoleService _tenantAdminRoleService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminRolesController(
        ITenantAdminRoleService tenantAdminRoleService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminRoleService = tenantAdminRoleService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.ListAsync(context, search, status, page, pageSize, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.GetByIdAsync(context, roleId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("roles/setup-options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSetupOptions(CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.GetSetupOptionsAsync(context, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("roles")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TenantAdminRoleCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var result = await _tenantAdminRoleService.CreateAsync(context, request, cancellationToken, idempotencyKey);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(nameof(GetById), new { roleId = result.Value.RoleId }, new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpPut("roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid roleId,
        [FromBody] TenantAdminRoleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.UpdateAsync(context, roleId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("roles/{roleId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(
        Guid roleId,
        [FromBody] TenantAdminRoleStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.UpdateStatusAsync(context, roleId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid roleId,
        [FromQuery] DateTimeOffset? expectedUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.DeleteAsync(context, roleId, expectedUpdatedAt, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpGet("permission-catalog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.GetPermissionCatalogAsync(context, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("roles/{roleId:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.GetPermissionsAsync(context, roleId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("roles/{roleId:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplacePermissions(
        Guid roleId,
        [FromBody] TenantRolePermissionsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.ReplacePermissionsAsync(context, roleId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("roles/{roleId:guid}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.GetAssignmentsAsync(context, roleId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("roles/{roleId:guid}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.GetAssignmentsAsync(context, roleId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("roles/{roleId:guid}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceAssignments(
        Guid roleId,
        [FromBody] TenantRoleAssignmentsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.ReplaceAssignmentsAsync(context, roleId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("roles/{roleId:guid}/setup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveSetup(
        Guid roleId,
        [FromBody] TenantRoleSetupSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryCreateContext(out var context, out var unauthorized)) return unauthorized!;

        var result = await _tenantAdminRoleService.SaveSetupAsync(context, roleId, request, cancellationToken);
        return ToActionResult(result);
    }

    private bool TryCreateContext(out TenantRequestContext context, out IActionResult? unauthorized)
    {
        if (_tenantRequestContextFactory.TryCreate(User, out context))
        {
            unauthorized = null;
            return true;
        }

        unauthorized = Unauthorized(CreateError(new ApplicationError(
            "tenant_roles.invalid_tenant_context",
            "Invalid tenant context.")));
        return false;
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null) return Ok(new { data = result.Value });
        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "tenant_roles.permission_denied" or "tenant_roles.delegation_ceiling_exceeded" =>
                StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "tenant_roles.not_found" or "tenant_roles.user_not_found" or "tenant_roles.outlet_not_found" =>
                NotFound(CreateError(error)),
            "tenant_roles.concurrency_conflict" or "tenant_roles.last_admin_protected" or
                "tenant_roles.duplicate_role_code" or "tenant_roles.duplicate_role_name" or
                "user.idempotency_conflict" or "user.idempotency_in_progress" =>
                Conflict(CreateError(error)),
            "tenant_roles.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new
        {
            code = error.Code,
            message = error.Message,
            details = error.FieldErrors?
                .Select(item => new { field = item.Field, message = item.Message })
                .ToArray<object>() ?? Array.Empty<object>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
