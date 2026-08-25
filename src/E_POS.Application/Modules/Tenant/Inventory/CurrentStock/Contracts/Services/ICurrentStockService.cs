using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;

namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Services;

/// <summary>
/// Provides services for managing and viewing current stock levels and movements.
/// </summary>
public interface ICurrentStockService
{
    /// <summary>
    /// Retrieves a high-level summary of the current stock (total items, low stock items, etc.).
    /// </summary>
    Task<ApplicationResult<CurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(
        TenantRequestContext context,
        Guid? outletId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a paginated list of current stock balances across products and locations.
    /// </summary>
    Task<ApplicationResult<CurrentStockListResponse>> GetCurrentStockAsync(
        TenantRequestContext context,
        CurrentStockQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exports the current stock list to a CSV byte array.
    /// </summary>
    Task<ApplicationResult<byte[]>> ExportCurrentStockAsync(
        TenantRequestContext context,
        CurrentStockQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes a new stock-in movement, updating the inventory balance and recording the transaction.
    /// </summary>
    Task<ApplicationResult<StockInResponse>> StockInAsync(
        TenantRequestContext context,
        StockInRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StockAdjustmentResponse>> AdjustStockAsync(
        TenantRequestContext context,
        StockAdjustmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StockTransferResponse>> TransferStockAsync(
        TenantRequestContext context,
        StockTransferRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ProductStockDetailResponse>> GetProductStockDetailAsync(
        TenantRequestContext context,
        Guid productVariantId,
        Guid? outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<StockMovementHistoryListResponse>> GetStockMovementHistoryAsync(
        TenantRequestContext context,
        StockMovementHistoryQuery query,
        CancellationToken cancellationToken);
}
