using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.OutletTillDevice;

[ApiController]
[Route("api/v1/tenant-admin/outlets")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminOutletsController : ControllerBase
{
    private readonly ITenantAdminOutletService _tenantAdminOutletService;
    private readonly ITenantAdminTillService _tenantAdminTillService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminOutletsController(
        ITenantAdminOutletService tenantAdminOutletService,
        ITenantAdminTillService tenantAdminTillService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _tenantAdminOutletService = tenantAdminOutletService;
        _tenantAdminTillService = tenantAdminTillService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? outletType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? operationalHealth = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var query = new TenantAdminOutletListQuery(
            pageNumber, pageSize, search, outletType, status, operationalHealth, sortBy, sortDirection);
        var result = await _tenantAdminOutletService.ListAsync(context, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminTillService.GetOutletOptionsAsync(context, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.GetDetailAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/revenue-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRevenueSummary(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.GetRevenueSummaryAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsers(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.GetUsersAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/tills")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTills(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.GetTillsAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOverview(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.GetOverviewAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] TenantAdminOutletStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.UpdateStatusAsync(context, id, request, cancellationToken);
        return ToEmptyActionResult(result);
    }

    [HttpPut("{id:guid}/manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetManager(
        Guid id,
        [FromBody] TenantAdminOutletManagerUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.SetManagerAsync(context, id, request, cancellationToken);
        return ToEmptyActionResult(result);
    }

    [HttpDelete("{id:guid}/manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveManager(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.RemoveManagerAsync(context, id, cancellationToken);
        return ToEmptyActionResult(result);
    }

    [HttpPut("{id:guid}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetImage(
        Guid id,
        [FromBody] TenantAdminOutletImageUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.SetImageAsync(context, id, request, cancellationToken);
        return ToEmptyActionResult(result);
    }

    [HttpDelete("{id:guid}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "outlet.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminOutletService.RemoveImageAsync(context, id, cancellationToken);
        return ToEmptyActionResult(result);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToEmptyActionResult(ApplicationResult result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "outlet.permission_denied" or "till.permission_denied" => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateError(error)),
            "outlet.not_found" or "tenant_user.not_found" or "media_asset.not_found" => NotFound(CreateError(error)),
            "outlet.invalid_tenant_context" => Unauthorized(CreateError(error)),
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
