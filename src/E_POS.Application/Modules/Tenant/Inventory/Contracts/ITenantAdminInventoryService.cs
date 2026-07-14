using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.Inventory.Contracts;

public interface ITenantAdminInventoryService
{
    Task<ApplicationResult<TenantAdminCurrentStockListResponse>> GetCurrentStockAsync(
        TenantRequestContext context,
        TenantAdminCurrentStockQuery query,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminCurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(
        TenantRequestContext context,
        Guid? outletId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminStockInResponse>> StockInAsync(
        TenantRequestContext context,
        TenantAdminStockInRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantAdminInventoryVariantLookupResponse>> GetProductVariantsForStockInAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken);
}
