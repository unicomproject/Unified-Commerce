using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
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
            "outlet.permission_denied" or "till.permission_denied" => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateError(error)),
            "outlet.not_found" => NotFound(CreateError(error)),
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
