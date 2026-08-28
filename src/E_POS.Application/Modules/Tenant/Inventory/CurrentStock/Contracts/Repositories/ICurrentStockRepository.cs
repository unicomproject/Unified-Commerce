using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;

namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Repositories;

/// <summary>
/// Provides data access for viewing current stock levels and processing stock movements.
/// </summary>
public interface ICurrentStockRepository
{
    /// <summary>
    /// Retrieves a high-level summary of the current stock (total items, low stock items, etc.).
    /// </summary>
    Task<CurrentStockSummaryResponse> GetCurrentStockSummaryAsync(
        Guid tenantId,
        Guid? outletId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a paginated list of current stock balances across products and locations.
    /// </summary>
    Task<CurrentStockListResponse> GetCurrentStockAsync(
        Guid tenantId,
        CurrentStockQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes a new stock-in movement, updating the inventory balance and recording the transaction.
    /// </summary>
    Task<StockInResponse> StockInAsync(
        Guid tenantId,
        Guid userId,
        StockInRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
        
    Task<StockAdjustmentResponse> AdjustStockAsync(
        Guid tenantId,
        Guid userId,
        StockAdjustmentRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
        
    Task<StockTransferResponse> TransferStockAsync(
        Guid tenantId,
        Guid userId,
        StockTransferRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
        
    /// <summary>
    /// Processes an opening stock movement.
    /// </summary>
    Task<OpeningStockResponse> AddOpeningStockAsync(
        Guid tenantId,
        Guid userId,
        OpeningStockRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
        
    /// <summary>
    /// Verifies if the specified outlet ID exists for the tenant.
    /// </summary>
    Task<bool> OutletExistsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies if the idempotency key has already been processed for stock movements.
    /// </summary>
    Task<bool> IdempotencyKeyExistsAsync(
        Guid tenantId,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<ProductStockDetailResponse?> GetProductStockDetailAsync(
        Guid tenantId,
        Guid productVariantId,
        Guid? outletId,
        CancellationToken cancellationToken);

    Task<StockMovementHistoryListResponse> GetStockMovementHistoryAsync(
        Guid tenantId,
        StockMovementHistoryQuery query,
        CancellationToken cancellationToken);
}
