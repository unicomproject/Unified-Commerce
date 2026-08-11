using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;

namespace E_POS.Application.Modules.Tenant.Inventory.Shared.Contracts;

public interface IInventoryRequestValidator
{
    ApplicationError? ValidateCurrentStockQuery(CurrentStockQuery query);
    ApplicationError? ValidateStockIn(StockInRequest request);
    ApplicationError? ValidateOpeningStock(OpeningStockRequest request);
}
