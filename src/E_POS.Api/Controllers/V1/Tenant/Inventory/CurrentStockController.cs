using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[Route("api/v1/tenant-admin/inventory/current-stock")]
public sealed class CurrentStockController : InventoryBaseController
{
    private readonly ICurrentStockService _currentStockService;
    private readonly ITenantRequestContextFactory _requestContextFactory;

    public CurrentStockController(
        ICurrentStockService currentStockService,
        ITenantRequestContextFactory requestContextFactory)
    {
        _currentStockService = currentStockService;
        _requestContextFactory = requestContextFactory;
    }

    [HttpGet]
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

    [HttpGet("export")]
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

    [HttpGet("summary")]
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

    [HttpGet("{id}/detail")]
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

    [HttpGet("{id}/movements")]
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
}
