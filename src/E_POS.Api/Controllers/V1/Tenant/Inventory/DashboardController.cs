using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[Route("api/v1/tenant-admin/inventory/dashboard")]
public sealed class DashboardController : InventoryBaseController
{
    private readonly IDashboardService _dashboardService;
    private readonly ITenantRequestContextFactory _requestContextFactory;

    public DashboardController(
        IDashboardService dashboardService,
        ITenantRequestContextFactory requestContextFactory)
    {
        _dashboardService = dashboardService;
        _requestContextFactory = requestContextFactory;
    }

    [HttpGet("metrics")]
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

    [HttpGet("alerts")]
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

    [HttpGet("activities")]
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
}
