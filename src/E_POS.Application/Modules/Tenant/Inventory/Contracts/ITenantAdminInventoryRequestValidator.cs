using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.Inventory.Contracts;

public interface ITenantAdminInventoryRequestValidator
{
    ApplicationError? ValidateCurrentStockQuery(TenantAdminCurrentStockQuery query);

    ApplicationError? ValidateStockIn(TenantAdminStockInRequest request);
}
