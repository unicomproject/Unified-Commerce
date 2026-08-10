using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.StockIn;

namespace E_POS.Application.Modules.Tenant.Inventory.Validators;

public sealed class InventoryRequestValidator : IInventoryRequestValidator
{
    public ApplicationError? ValidateCurrentStockQuery(CurrentStockQuery query)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (query.Page <= 0)
            fieldErrors.Add(new ApplicationFieldError("page", "Page must be greater than zero."));

        if (query.PageSize <= 0 || query.PageSize > 100)
            fieldErrors.Add(new ApplicationFieldError("pageSize", "Page size must be between 1 and 100."));

        if (!string.IsNullOrWhiteSpace(query.Search) && query.Search.Length > 100)
            fieldErrors.Add(new ApplicationFieldError("search", "Search term is too long."));

        return fieldErrors.Count > 0
            ? new ApplicationError("inventory.validation_failed", "Validation failed.", fieldErrors)
            : null;
    }

    public ApplicationError? ValidateStockIn(StockInRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (request.OutletId == Guid.Empty)
            fieldErrors.Add(new ApplicationFieldError("outletId", "Outlet is required."));

        if (request.Items is null || request.Items.Count == 0)
            fieldErrors.Add(new ApplicationFieldError("items", "At least one item is required for stock-in."));
        else if (request.Items.Count > 200)
            fieldErrors.Add(new ApplicationFieldError("items", "Cannot process more than 200 items in a single request."));

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && request.IdempotencyKey.Length > 100)
            fieldErrors.Add(new ApplicationFieldError("idempotencyKey", "Idempotency key must be 100 characters or less."));

        if (request.Items != null)
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                if (item.ProductId == Guid.Empty)
                    fieldErrors.Add(new ApplicationFieldError($"items[{i}].productId", "Product is required."));
                
                if (item.Quantity <= 0)
                    fieldErrors.Add(new ApplicationFieldError($"items[{i}].quantity", "Quantity must be greater than zero."));
                
                if (item.UnitCost < 0)
                    fieldErrors.Add(new ApplicationFieldError($"items[{i}].unitCost", "Unit cost cannot be negative."));
            }
        }

        return fieldErrors.Count > 0
            ? new ApplicationError("inventory.validation_failed", "Validation failed.", fieldErrors)
            : null;
    }
}
