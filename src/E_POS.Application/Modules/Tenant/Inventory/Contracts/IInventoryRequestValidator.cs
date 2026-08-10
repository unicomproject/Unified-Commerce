using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.StockIn;

namespace E_POS.Application.Modules.Tenant.Inventory.Contracts;

public interface IInventoryRequestValidator
{
    ApplicationError? ValidateCurrentStockQuery(CurrentStockQuery query);
    ApplicationError? ValidateStockIn(StockInRequest request);
}
