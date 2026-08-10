using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Contracts.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Contracts.Dashboard;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.StockIn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[ApiController]
[Route("api/v1/tenant-admin/inventory")]
[Authorize(Policy = "TenantOnly")]
[Tags("Tenant Admin - Inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentStockService _currentStockService;
    private readonly ITenantRequestContextFactory _requestContextFactory;

    public InventoryController(
        IDashboardService dashboardService,
        ICurrentStockService currentStockService,
        ITenantRequestContextFactory requestContextFactory)
    {
        _dashboardService = dashboardService;
        _currentStockService = currentStockService;
        _requestContextFactory = requestContextFactory;
    }

    [HttpGet("dashboard/metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardMetrics(
        [FromQuery] Guid? outletId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _dashboardService.GetDashboardMetricsAsync(context, outletId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("dashboard/alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardAlerts(
        [FromQuery] Guid? outletId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _dashboardService.GetDashboardAlertsAsync(context, outletId, page, pageSize, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("dashboard/activities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardActivities(
        [FromQuery] Guid? outletId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _dashboardService.GetDashboardActivitiesAsync(context, outletId, page, pageSize, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("current-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentStock(
        [FromQuery] CurrentStockQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _currentStockService.GetCurrentStockAsync(context, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("current-stock/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCurrentStock(
        [FromQuery] CurrentStockQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _currentStockService.ExportCurrentStockAsync(context, query, cancellationToken);
        if (!result.IsSuccess) return ToActionResult(result);
        
        return File(result.Value, "text/csv", $"current_stock_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet("current-stock/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentStockSummary(
        [FromQuery] Guid? outletId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _currentStockService.GetCurrentStockSummaryAsync(context, outletId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("stock-in")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> StockIn(
        [FromBody] StockInRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _currentStockService.StockInAsync(context, request, cancellationToken);
        
        if (result.IsSuccess && result.Value is not null)
        {
            return Created($"/api/v1/tenant-admin/inventory/current-stock/{result.Value.StockMovementId}", new { data = result.Value });
        }
        
        return ToErrorResult(result.Error);
    }

    [HttpGet("current-stock/{id}/detail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentStockDetail(
        [FromRoute] Guid id,
        [FromQuery] Guid? outletId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _currentStockService.GetProductStockDetailAsync(context, id, outletId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("current-stock/{id}/movements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockMovements(
        [FromRoute] Guid id,
        [FromQuery] StockMovementHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        query.ProductVariantId = id;
        var result = await _currentStockService.GetStockMovementHistoryAsync(context, query, cancellationToken);
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
            "inventory.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "inventory.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "inventory.validation_failed" => BadRequest(CreateError(error)),
            "inventory.duplicate_request" => Conflict(CreateError(error)),
            "inventory.not_found" => NotFound(CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private object CreateError(ApplicationError error)
    {
        var fieldErrors = error.FieldErrors?
            .Select(item => new { field = item.Field, message = item.Message })
            .ToArray<object>() ?? Array.Empty<object>();

        return new
        {
            code = error.Code,
            message = error.Message,
            details = fieldErrors,
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
