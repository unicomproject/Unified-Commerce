using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;

namespace E_POS.Application.Modules.Tenant.Inventory.Validators;

public sealed class TenantAdminInventoryRequestValidator : ITenantAdminInventoryRequestValidator
{
    private const int MaxPageSize = 100;
    private const int MaxReferenceNumberLength = 100;
    private const int MaxNotesLength = 500;
    private const int MaxBatchNumberLength = 100;
    private const int MaxIdempotencyKeyLength = 100;
    private const int MaxStockInLines = 100;

    private static readonly HashSet<string> SupportedStockStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        TenantAdminInventoryConstants.StockStatusInStock,
        TenantAdminInventoryConstants.StockStatusLowStock,
        TenantAdminInventoryConstants.StockStatusOutOfStock,
        TenantAdminInventoryConstants.StockStatusFilterAll,
    };

    private static readonly HashSet<string> SupportedExpiryStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        TenantAdminInventoryConstants.ExpiryFilterExpiring,
        TenantAdminInventoryConstants.ExpiryFilterExpired,
        TenantAdminInventoryConstants.ExpiryFilterAll,
    };

    private static readonly HashSet<string> SupportedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "productName",
        "onHandQuantity",
        "availableQuantity",
        "expiryDate",
    };

    public ApplicationError? ValidateCurrentStockQuery(TenantAdminCurrentStockQuery query)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (query.Page < 1)
        {
            fieldErrors.Add(new ApplicationFieldError("page", "Page must be at least 1."));
        }

        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
        {
            fieldErrors.Add(new ApplicationFieldError("pageSize", $"Page size must be between 1 and {MaxPageSize}."));
        }

        if (!string.IsNullOrWhiteSpace(query.StockStatus) &&
            !SupportedStockStatuses.Contains(query.StockStatus.Trim()))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "stockStatus",
                "Stock status must be IN_STOCK, LOW_STOCK, OUT_OF_STOCK, or ALL."));
        }

        if (!string.IsNullOrWhiteSpace(query.ExpiryStatus) &&
            !SupportedExpiryStatuses.Contains(query.ExpiryStatus.Trim()))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "expiryStatus",
                "Expiry status must be EXPIRING, EXPIRED, or ALL."));
        }

        if (!string.IsNullOrWhiteSpace(query.SortBy) &&
            !SupportedSortFields.Contains(query.SortBy.Trim()))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "sortBy",
                "Sort by must be productName, onHandQuantity, availableQuantity, or expiryDate."));
        }

        if (!string.IsNullOrWhiteSpace(query.SortDirection) &&
            !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            fieldErrors.Add(new ApplicationFieldError("sortDirection", "Sort direction must be asc or desc."));
        }

        return fieldErrors.Count == 0
            ? null
            : new ApplicationError("inventory.validation_failed", "Inventory validation failed.", fieldErrors);
    }

    public ApplicationError? ValidateStockIn(TenantAdminStockInRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (request.OutletId == Guid.Empty)
        {
            fieldErrors.Add(new ApplicationFieldError("outletId", "Outlet is required."));
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            fieldErrors.Add(new ApplicationFieldError("items", "At least one stock-in line is required."));
        }
        else if (request.Items.Count > MaxStockInLines)
        {
            fieldErrors.Add(new ApplicationFieldError("items", $"Stock-in supports up to {MaxStockInLines} lines."));
        }
        else
        {
            for (var index = 0; index < request.Items.Count; index++)
            {
                ValidateStockInLine(request.Items[index], index, fieldErrors);
            }

            ValidateDuplicateLines(request.Items, fieldErrors);
        }

        if (!string.IsNullOrWhiteSpace(request.ReferenceNumber) &&
            request.ReferenceNumber.Trim().Length > MaxReferenceNumberLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "referenceNumber",
                $"Reference number must be {MaxReferenceNumberLength} characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > MaxNotesLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "notes",
                $"Notes must be {MaxNotesLength} characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) &&
            request.IdempotencyKey.Trim().Length > MaxIdempotencyKeyLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "idempotencyKey",
                $"Idempotency key must be {MaxIdempotencyKeyLength} characters or less."));
        }

        return fieldErrors.Count == 0
            ? null
            : new ApplicationError("inventory.validation_failed", "Inventory validation failed.", fieldErrors);
    }

    private static void ValidateDuplicateLines(
        IReadOnlyList<TenantAdminStockInLineRequest> items,
        ICollection<ApplicationFieldError> fieldErrors)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < items.Count; index++)
        {
            var line = items[index];
            if (line.ProductVariantId == Guid.Empty)
            {
                continue;
            }

            var batchKey = string.IsNullOrWhiteSpace(line.BatchNumber)
                ? string.Empty
                : line.BatchNumber.Trim().ToUpperInvariant();
            var key = $"{line.ProductVariantId:N}|{batchKey}";
            if (!seen.Add(key))
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"items[{index}].productVariantId",
                    "Duplicate variant and batch combination is not allowed in the same stock-in request."));
            }
        }
    }

    private static void ValidateStockInLine(
        TenantAdminStockInLineRequest line,
        int index,
        ICollection<ApplicationFieldError> fieldErrors)
    {
        var prefix = $"items[{index}]";

        if (line.ProductVariantId == Guid.Empty)
        {
            fieldErrors.Add(new ApplicationFieldError($"{prefix}.productVariantId", "Product variant is required."));
        }

        if (line.Quantity <= 0)
        {
            fieldErrors.Add(new ApplicationFieldError($"{prefix}.quantity", "Quantity must be greater than zero."));
        }

        if (line.UnitCost is < 0)
        {
            fieldErrors.Add(new ApplicationFieldError($"{prefix}.unitCost", "Unit cost cannot be negative."));
        }

        if (!string.IsNullOrWhiteSpace(line.BatchNumber) &&
            line.BatchNumber.Trim().Length > MaxBatchNumberLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                $"{prefix}.batchNumber",
                $"Batch number must be {MaxBatchNumberLength} characters or less."));
        }

        if (line.ManufacturedDate.HasValue &&
            line.ExpiryDate.HasValue &&
            line.ExpiryDate.Value < line.ManufacturedDate.Value)
        {
            fieldErrors.Add(new ApplicationFieldError(
                $"{prefix}.expiryDate",
                "Expiry date cannot be earlier than manufactured date."));
        }
    }
}
