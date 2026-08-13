using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;

namespace E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Contracts.Services;

public interface IOpeningStockService
{
    Task<ApplicationResult<OpeningStockResponse>> AddOpeningStockAsync(
        TenantRequestContext context,
        OpeningStockRequest request,
        CancellationToken cancellationToken);
}
