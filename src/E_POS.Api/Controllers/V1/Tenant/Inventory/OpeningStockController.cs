using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[Route("api/v1/tenant-admin/inventory/opening-stock")]
public sealed class OpeningStockController : InventoryBaseController
{
    private readonly IOpeningStockService _openingStockService;
    private readonly ITenantRequestContextFactory _requestContextFactory;

    public OpeningStockController(
        IOpeningStockService openingStockService,
        ITenantRequestContextFactory requestContextFactory)
    {
        _openingStockService = openingStockService;
        _requestContextFactory = requestContextFactory;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddOpeningStock(
        [FromBody] OpeningStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_requestContextFactory.TryCreate(User, out var context))
            return Unauthorized(new ApplicationError("auth.unauthorized", "User is not authorized."));

        var result = await _openingStockService.AddOpeningStockAsync(context, request, cancellationToken);
        
        if (result.IsSuccess && result.Value is not null)
        {
            return Created($"/api/v1/tenant-admin/inventory/current-stock/{result.Value.StockMovementId}", new { data = result.Value });
        }
        
        return ToErrorResult(result.Error);
    }
}
