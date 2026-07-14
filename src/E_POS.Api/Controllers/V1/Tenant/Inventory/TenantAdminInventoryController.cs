using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;
using E_POS.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.Inventory;

[ApiController]
[Route("api/v1/tenant-admin/inventory")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminInventoryController : ControllerBase
{
    private readonly ITenantAdminInventoryService _inventoryService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public TenantAdminInventoryController(
        ITenantAdminInventoryService inventoryService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _inventoryService = inventoryService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet("current-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentStock(
        [FromQuery] Guid? outletId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? batchNumber = null,
        [FromQuery] string? expiryStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "inventory.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var query = new TenantAdminCurrentStockQuery(
            outletId,
            search,
            stockStatus,
            categoryId,
            batchNumber,
            expiryStatus,
            page,
            pageSize,
            sortBy,
            sortDirection);

        var result = await _inventoryService.GetCurrentStockAsync(context, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("current-stock/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentStockSummary(
        [FromQuery] Guid? outletId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "inventory.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _inventoryService.GetCurrentStockSummaryAsync(context, outletId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("stock-in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StockIn(
        [FromBody] TenantAdminStockInRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "inventory.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _inventoryService.StockInAsync(context, request, cancellationToken);
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
            "inventory.permission_denied" or "inventory.outlet_access_denied" or "inventory.tenant_blocked" =>
                StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "inventory.invalid_tenant_context" => Unauthorized(CreateError(error)),
            "inventory.validation_failed" => BadRequest(CreateError(error)),
            "inventory.not_found" or "inventory.outlet_not_found" => NotFound(CreateError(error)),
            "inventory.location_not_found" => UnprocessableEntity(CreateError(error)),
            "inventory.duplicate_idempotency_key" or "inventory.concurrency_conflict" =>
                StatusCode(StatusCodes.Status409Conflict, CreateError(error)),
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
