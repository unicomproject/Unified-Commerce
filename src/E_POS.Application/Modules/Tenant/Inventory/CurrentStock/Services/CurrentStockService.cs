using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Shared.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;

namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Services;

public sealed class CurrentStockService : ICurrentStockService
{
    private readonly ICurrentStockRepository _repository;
    private readonly IInventoryRequestValidator _validator;
    private readonly IInventoryAuditLogger _auditLogger;

    public CurrentStockService(
        ICurrentStockRepository repository,
        IInventoryRequestValidator validator,
        IInventoryAuditLogger auditLogger)
    {
        _repository = repository;
        _validator = validator;
        _auditLogger = auditLogger;
    }

    public async Task<ApplicationResult<CurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(
        TenantRequestContext context,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.View) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<CurrentStockSummaryResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        var result = await _repository.GetCurrentStockSummaryAsync(context.TenantId, outletId, cancellationToken);
        return ApplicationResult<CurrentStockSummaryResponse>.Success(result);
    }

    public async Task<ApplicationResult<CurrentStockListResponse>> GetCurrentStockAsync(
        TenantRequestContext context,
        CurrentStockQuery query,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.View) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<CurrentStockListResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        var error = _validator.ValidateCurrentStockQuery(query);
        if (error != null)
            return ApplicationResult<CurrentStockListResponse>.Failure(error);

        var result = await _repository.GetCurrentStockAsync(context.TenantId, query, cancellationToken);
        return ApplicationResult<CurrentStockListResponse>.Success(result);
    }

    public async Task<ApplicationResult<byte[]>> ExportCurrentStockAsync(
        TenantRequestContext context,
        CurrentStockQuery query,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.View) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<byte[]>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        var exportQuery = new CurrentStockQuery
        {
            OutletId = query.OutletId,
            Search = query.Search,
            StockStatus = query.StockStatus,
            CategoryId = query.CategoryId,
            BatchNumber = query.BatchNumber,
            ExpiryStatus = query.ExpiryStatus,
            SortBy = query.SortBy,
            SortDirection = query.SortDirection,
            Page = 1,
            PageSize = 10000
        };
        var result = await _repository.GetCurrentStockAsync(context.TenantId, exportQuery, cancellationToken);
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Product Name,SKU,Barcode,On Hand,Reserved,Available,Status,Reorder Level,Last Movement");
        foreach (var item in result.Items)
        {
            var name = item.ProductName?.Replace(",", " ") ?? "";
            var sku = item.Sku?.Replace(",", " ") ?? "";
            var barcode = item.Barcode?.Replace(",", " ") ?? "";
            var lastMove = item.LastMovementAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            sb.AppendLine($"{name},{sku},{barcode},{item.OnHandQuantity},{item.ReservedQuantity},{item.AvailableQuantity},{item.StockStatus},{item.ReorderLevel ?? 0},{lastMove}");
        }

        return ApplicationResult<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task<ApplicationResult<StockInResponse>> StockInAsync(
        TenantRequestContext context,
        StockInRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.StockIn))
            return ApplicationResult<StockInResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied for processing stock in."));

        var validationError = _validator.ValidateStockIn(request);
        if (validationError != null)
            return ApplicationResult<StockInResponse>.Failure(validationError);

        var outletExists = await _repository.OutletExistsAsync(context.TenantId, request.OutletId, cancellationToken);
        if (!outletExists)
            return ApplicationResult<StockInResponse>.Failure(new ApplicationError("inventory.outlet_not_found", "The specified outlet was not found."));

        var isIdempotent = await _repository.IdempotencyKeyExistsAsync(context.TenantId, request.IdempotencyKey, cancellationToken);
        if (isIdempotent)
            return ApplicationResult<StockInResponse>.Failure(new ApplicationError("inventory.duplicate_request", "This stock-in request has already been processed."));

        var response = await _repository.StockInAsync(context.TenantId, context.UserId, request, DateTimeOffset.UtcNow, cancellationToken);
        
        _auditLogger.LogStockInCompleted(context.TenantId, context.UserId, response.StockMovementId, response.OutletId);
        
        return ApplicationResult<StockInResponse>.Success(response);
    }

    public async Task<ApplicationResult<ProductStockDetailResponse>> GetProductStockDetailAsync(
        TenantRequestContext context,
        Guid productVariantId,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.View) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<ProductStockDetailResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        var result = await _repository.GetProductStockDetailAsync(context.TenantId, productVariantId, outletId, cancellationToken);
        if (result == null)
            return ApplicationResult<ProductStockDetailResponse>.Failure(new ApplicationError("inventory.not_found", "Product variant not found."));
            
        return ApplicationResult<ProductStockDetailResponse>.Success(result);
    }

    public async Task<ApplicationResult<StockMovementHistoryListResponse>> GetStockMovementHistoryAsync(
        TenantRequestContext context,
        StockMovementHistoryQuery query,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.View) && !context.HasPermission(StockPermissions.LegacyInventoryView))
            return ApplicationResult<StockMovementHistoryListResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied."));

        var result = await _repository.GetStockMovementHistoryAsync(context.TenantId, query, cancellationToken);
        return ApplicationResult<StockMovementHistoryListResponse>.Success(result);
    }
}
