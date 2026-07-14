using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class TenantAdminProductRequestValidator : ITenantAdminProductRequestValidator
{
    public ApplicationError? ValidateCreate(TenantAdminProductCreateRequest request) =>
        ValidateWrite(request, isCreate: true);

    public ApplicationError? ValidateUpdate(TenantAdminProductCreateRequest request) =>
        ValidateWrite(request, isCreate: false);

    public ApplicationError? ValidateStatusUpdate(TenantAdminProductStatusUpdateRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            fieldErrors.Add(new ApplicationFieldError("status", "Status is required."));
        }
        else if (!ProductConstants.IsValidWriteStatus(request.Status))
        {
            fieldErrors.Add(new ApplicationFieldError("status", "Status must be Active or Inactive."));
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return new ApplicationError(
            "product.validation_failed",
            "Product validation failed.",
            fieldErrors);
    }

    private static ApplicationError? ValidateWrite(TenantAdminProductCreateRequest request, bool isCreate)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            fieldErrors.Add(new ApplicationFieldError("productName", "Product name is required."));
        }
        else if (request.ProductName.Trim().Length > 200)
        {
            fieldErrors.Add(new ApplicationFieldError("productName", "Product name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            fieldErrors.Add(new ApplicationFieldError("sku", "SKU is required."));
        }
        else if (request.Sku.Trim().Length > 255)
        {
            fieldErrors.Add(new ApplicationFieldError("sku", "SKU cannot exceed 255 characters."));
        }

        if (request.CategoryId == Guid.Empty)
        {
            fieldErrors.Add(new ApplicationFieldError("categoryId", "Category is required."));
        }

        if (string.IsNullOrWhiteSpace(request.UnitType))
        {
            fieldErrors.Add(new ApplicationFieldError("unitType", "Unit type is required."));
        }

        if (isCreate)
        {
            if (request.SellingPrice <= 0)
            {
                fieldErrors.Add(new ApplicationFieldError("sellingPrice", "Selling price is required."));
            }
        }
        else if (request.SellingPrice < 0)
        {
            fieldErrors.Add(new ApplicationFieldError("sellingPrice", "Selling price must be zero or greater."));
        }

        if (request.Barcode != null && request.Barcode.Trim().Length > 255)
        {
            fieldErrors.Add(new ApplicationFieldError("barcode", "Barcode cannot exceed 255 characters."));
        }

        if (request.CostPrice.HasValue && request.CostPrice.Value < 0)
        {
            fieldErrors.Add(new ApplicationFieldError("costPrice", "Cost price cannot be negative."));
        }

        if (request.DiscountPrice.HasValue && request.DiscountPrice.Value < 0)
        {
            fieldErrors.Add(new ApplicationFieldError("discountPrice", "Discount price cannot be negative."));
        }

        if (request.DiscountPrice.HasValue && request.DiscountPrice.Value > request.SellingPrice)
        {
            fieldErrors.Add(new ApplicationFieldError("discountPrice", "Discount price cannot exceed selling price."));
        }

        if (!request.SaveAsDraft)
        {
            if (string.IsNullOrWhiteSpace(request.Status) ||
                !ProductConstants.IsValidWriteStatus(request.Status))
            {
                fieldErrors.Add(new ApplicationFieldError("status", "Status must be Active or Inactive."));
            }
        }

        if (request.TrackInventory)
        {
            if (request.OutletIds == null || request.OutletIds.Count == 0)
            {
                fieldErrors.Add(new ApplicationFieldError("outletIds", "At least one outlet is required when tracking inventory."));
            }

            if (!request.OpeningStockQuantity.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "openingStockQuantity",
                    "Opening stock quantity is required when tracking inventory."));
            }
            else if (request.OpeningStockQuantity.Value < 0)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "openingStockQuantity",
                    "Opening stock quantity cannot be negative."));
            }

            if (!request.MinimumStockAlertQuantity.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "minimumStockAlertQuantity",
                    "Minimum stock alert quantity is required when tracking inventory."));
            }
            else if (request.MinimumStockAlertQuantity.Value < 0)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "minimumStockAlertQuantity",
                    "Minimum stock alert quantity cannot be negative."));
            }

            if (request.MaximumStockQuantity.HasValue &&
                request.MaximumStockQuantity.Value < 0)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "maximumStockQuantity",
                    "Maximum stock quantity cannot be negative."));
            }

            if (string.IsNullOrWhiteSpace(request.StockUnit))
            {
                fieldErrors.Add(new ApplicationFieldError("stockUnit", "Stock unit is required when tracking inventory."));
            }
        }

        if (request.HasVariants)
        {
            if (request.Variants == null || request.Variants.Count == 0)
            {
                fieldErrors.Add(new ApplicationFieldError("variants", "At least one variant is required."));
            }
            else
            {
                for (var index = 0; index < request.Variants.Count; index++)
                {
                    var variant = request.Variants[index];
                    var prefix = $"variants[{index}]";

                    if (string.IsNullOrWhiteSpace(variant.Sku))
                    {
                        fieldErrors.Add(new ApplicationFieldError($"{prefix}.sku", "Variant SKU is required."));
                    }

                    if (isCreate && variant.SellingPrice <= 0)
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"{prefix}.sellingPrice",
                            "Variant selling price is required."));
                    }
                    else if (!isCreate && variant.SellingPrice < 0)
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"{prefix}.sellingPrice",
                            "Variant selling price must be zero or greater."));
                    }

                    if (variant.DiscountPrice.HasValue && variant.DiscountPrice.Value > variant.SellingPrice)
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"{prefix}.discountPrice",
                            "Variant discount price cannot exceed selling price."));
                    }
                }
            }
        }

        if (request.HasExpiryDate)
        {
            if (string.IsNullOrWhiteSpace(request.BatchNumber))
            {
                fieldErrors.Add(new ApplicationFieldError("batchNumber", "Batch number is required when expiry tracking is enabled."));
            }

            if (!request.ExpiryDate.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError("expiryDate", "Expiry date is required when expiry tracking is enabled."));
            }
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return new ApplicationError(
            "product.validation_failed",
            "Product validation failed.",
            fieldErrors);
    }
}
