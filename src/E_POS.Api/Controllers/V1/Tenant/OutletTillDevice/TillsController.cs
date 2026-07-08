using System.Security.Claims;
using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tills")]
public sealed class TillsController : ControllerBase
{
    private readonly ITillService _tillService;

    public TillsController(ITillService tillService)
    {
        _tillService = tillService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TillResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] TillCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantRequestContext(out var context)) return Unauthorized(CreateError(new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _tillService.CreateAsync(context, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(typeof(TillListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List([FromQuery] Guid? outletId = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantRequestContext(out var context)) return Unauthorized(CreateError(new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _tillService.ListAsync(context, outletId, pageNumber, pageSize, search, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantRequestContext(out var context)) return Unauthorized(CreateError(new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _tillService.GetByIdAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TillUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantRequestContext(out var context)) return Unauthorized(CreateError(new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _tillService.UpdateAsync(context, id, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetTenantRequestContext(out var context)) return Unauthorized(CreateError(new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _tillService.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "till.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "till.not_found" or "till.outlet_not_found" => NotFound(CreateError(error)),
            "till.duplicate_code" or "till.delete_conflict" => Conflict(CreateError(error)),
            "till.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private bool TryGetTenantRequestContext(out TenantRequestContext context)
    {
        var tenantUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = User.FindFirstValue("tenant_id");
        var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out var tenantUserId);
        var hasTenantId = Guid.TryParse(tenantIdValue, out var tenantId);
        context = new TenantRequestContext(tenantId, tenantUserId, GetPermissionClaims());
        return hasTenantUserId && hasTenantId;
    }

    private IReadOnlyCollection<string> GetPermissionClaims()
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in User.FindAll("permissions"))
        {
            if (string.IsNullOrWhiteSpace(claim.Value)) continue;
            if (claim.Value.TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                foreach (var permission in JsonSerializer.Deserialize<string[]>(claim.Value) ?? []) permissions.Add(permission);
                continue;
            }

            permissions.Add(claim.Value);
        }

        return permissions;
    }

    private object CreateError(ApplicationError error)
    {
        return new { code = error.Code, message = error.Message, details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow };
    }
}

