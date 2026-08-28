using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[Route("api/v1/tenant-admin/inventory/stock-transfers")]
public sealed class StockTransfersController : InventoryBaseController
{
    private readonly ICurrentStockService _currentStockService;
    private readonly ITenantRequestContextFactory _requestContextFactory;

    public StockTransfersController(
        ICurrentStockService currentStockService,
        ITenantRequestContextFactory requestContextFactory)
    {
        _currentStockService = currentStockService;
        _requestContextFactory = requestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StockTransferResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> TransferStock(
        [FromBody] StockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _currentStockService.TransferStockAsync(context, request, cancellationToken);
        
        if (result.IsSuccess && result.Value is not null)
        {
            return Created($"/api/v1/tenant-admin/inventory/current-stock/{result.Value.StockMovementId}", new { data = result.Value });
        }
        
        return ToErrorResult(result.Error);
    }
}
