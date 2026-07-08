using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.OutletTillDevice;

[ApiController]
[Route("api/v1/tenant-admin/tills")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminTillsController : ControllerBase
{
    private readonly ITenantAdminTillService _tenantAdminTillService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminTillsController(
        ITenantAdminTillService tenantAdminTillService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminTillService = tenantAdminTillService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? outletId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "till.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.ListAsync(
            context,
            search,
            status,
            outletId,
            page,
            pageSize,
            sortBy,
            sortDirection,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "till.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.GetSummaryAsync(context, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] TenantAdminTillCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "till.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.CreateAsync(context, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value.TillId },
                new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "till.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.GetByIdAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TenantAdminTillUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "till.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.UpdateAsync(context, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "till.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "till.permission_denied" => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateError(error)),
            "till.not_found" or "till.outlet_not_found" => NotFound(CreateError(error)),
            "till.duplicate_code" or "till.delete_conflict" => Conflict(CreateError(error)),
            "till.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new
        {
            code = error.Code,
            message = error.Message,
            details = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
