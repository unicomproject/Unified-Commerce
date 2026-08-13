using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.Shared.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;

namespace E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Services;

public sealed class OpeningStockService : IOpeningStockService
{
    private readonly ICurrentStockRepository _repository;
    private readonly IInventoryRequestValidator _validator;
    private readonly IInventoryAuditLogger _auditLogger;

    public OpeningStockService(
        ICurrentStockRepository repository,
        IInventoryRequestValidator validator,
        IInventoryAuditLogger auditLogger)
    {
        _repository = repository;
        _validator = validator;
        _auditLogger = auditLogger;
    }

    public async Task<ApplicationResult<OpeningStockResponse>> AddOpeningStockAsync(
        TenantRequestContext context,
        OpeningStockRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(StockPermissions.OpeningStock))
            return ApplicationResult<OpeningStockResponse>.Failure(new ApplicationError("inventory.permission_denied", "Permission denied for adding opening stock."));

        var validationError = _validator.ValidateOpeningStock(request);
        if (validationError != null)
            return ApplicationResult<OpeningStockResponse>.Failure(validationError);

        var outletExists = await _repository.OutletExistsAsync(context.TenantId, request.OutletId, cancellationToken);
        if (!outletExists)
            return ApplicationResult<OpeningStockResponse>.Failure(new ApplicationError("inventory.outlet_not_found", "The specified outlet was not found."));

        var isIdempotent = await _repository.IdempotencyKeyExistsAsync(context.TenantId, request.IdempotencyKey, cancellationToken);
        if (isIdempotent)
            return ApplicationResult<OpeningStockResponse>.Failure(new ApplicationError("inventory.duplicate_request", "This opening stock request has already been processed."));

        // Enforce 0 stock rule for all requested items
        foreach (var item in request.Items)
        {
            var variantId = item.VariantId ?? item.ProductId; // Fallback to ProductId if no VariantId is provided (assuming single-variant product)
            var stockDetail = await _repository.GetProductStockDetailAsync(context.TenantId, variantId, request.OutletId, cancellationToken);
            
            if (stockDetail != null && stockDetail.TotalOnHand > 0)
            {
                return ApplicationResult<OpeningStockResponse>.Failure(new ApplicationError(
                    "inventory.opening_stock_invalid", 
                    $"Opening stock can only be added if current balance is 0. Product/Variant {variantId} has {stockDetail.TotalOnHand} on hand."));
            }
        }

        var response = await _repository.AddOpeningStockAsync(context.TenantId, context.UserId, request, DateTimeOffset.UtcNow, cancellationToken);
        
        _auditLogger.LogStockInCompleted(context.TenantId, context.UserId, response.StockMovementId, response.OutletId); // Reusing LogStockInCompleted for simplicity, or we could add LogOpeningStockCompleted
        
        return ApplicationResult<OpeningStockResponse>.Success(response);
    }
}
