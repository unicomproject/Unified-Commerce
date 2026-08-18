using System.Text.RegularExpressions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Services;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class TenantAdminProductRequestValidator : ITenantAdminProductRequestValidator
{
    private static readonly Regex AlphanumericDashRegex = new(
        @"^[A-Za-z0-9\-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    public ApplicationError? ValidateListQuery(
        string? productStatus,
        string? stockStatus,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (pageNumber < 1)
        {
            fieldErrors.Add(new ApplicationFieldError("pageNumber", "Page number must be 1 or greater."));
        }

        var allowedPageSizes = new[] { 6, 8, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize))
        {
            fieldErrors.Add(new ApplicationFieldError("pageSize", "Page size must be 6, 8, 10, 25, or 50."));
        }

        if (productStatus != null)
        {
            var allowedStatuses = new[] { "ACTIVE", "INACTIVE", "DRAFT" };
            if (!allowedStatuses.Contains(productStatus.ToUpperInvariant()))
            {
                fieldErrors.Add(new ApplicationFieldError("productStatus", "Product status must be ACTIVE, INACTIVE, or DRAFT."));
            }
        }

        if (stockStatus != null)
        {
            var allowedStockStatuses = new[] { "NOT_TRACKED", "IN_STOCK", "LOW_STOCK", "OUT_OF_STOCK" };
            if (!allowedStockStatuses.Contains(stockStatus.ToUpperInvariant()))
            {
                fieldErrors.Add(new ApplicationFieldError("stockStatus", "Stock status must be NOT_TRACKED, IN_STOCK, LOW_STOCK, or OUT_OF_STOCK."));
            }
        }

        if (sortBy != null)
        {
            var allowedSortFields = new[] { "PRODUCTNAME", "SKU", "CREATEDAT" };
            if (!allowedSortFields.Contains(sortBy.ToUpperInvariant()))
            {
                fieldErrors.Add(new ApplicationFieldError("sortBy", "Sort by field must be productName, sku, or createdAt."));
            }
        }

        if (sortDirection != null)
        {
            var allowedDirections = new[] { "ASC", "DESC" };
            if (!allowedDirections.Contains(sortDirection.ToUpperInvariant()))
            {
                fieldErrors.Add(new ApplicationFieldError("sortDirection", "Sort direction must be asc or desc."));
            }
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return new ApplicationError(
            "product.validation_failed",
            "Product list query validation failed.",
            fieldErrors);
    }

    public ApplicationError? ValidateSaveDraft(SaveProductDraftRequest request) =>
        ValidateStepSaveDraft(request);

    public ApplicationError? ValidateSaveAndContinue(SaveProductDraftRequest request) =>
        ValidateStepSaveAndContinue(request);

    public ApplicationError? ValidateDraft(SaveProductDraftRequest request, bool requireCategory) =>
        ValidateStep1Draft(request, requireProductName: requireCategory, requireCategory: requireCategory);

    public ApplicationError? ValidateStepSaveDraft(SaveProductDraftRequest request)
    {
        return request.CurrentSetupStep switch
        {
            ProductWizardStage.ProductTypeTracking => ValidateProductTypeTrackingSaveDraft(request),
            ProductWizardStage.UnitsPackConversion => ValidateUnitsPackConversionDraft(request),
            ProductWizardStage.ProductConfiguration => ValidateProductConfigurationDraft(request),
            ProductWizardStage.BarcodeSku => ValidateBarcodeSkuDraft(request),
            ProductWizardStage.PricingTax => ValidatePricingTaxDraft(request),
            _ => ValidateStep1Draft(request, requireProductName: false, requireCategory: false)
        };
    }

    public ApplicationError? ValidateStepSaveAndContinue(SaveProductDraftRequest request)
    {
        return request.CurrentSetupStep switch
        {
            ProductWizardStage.ProductTypeTracking => ValidateProductTypeTrackingContinue(request),
            ProductWizardStage.UnitsPackConversion => ValidateUnitsPackConversionContinue(request),
            ProductWizardStage.ProductConfiguration => ValidateProductConfigurationContinue(request),
            ProductWizardStage.BarcodeSku => ValidateBarcodeSkuContinue(request),
            ProductWizardStage.PricingTax => ValidatePricingTaxContinue(request),
            _ => ValidateStep1Draft(request, requireProductName: true, requireCategory: true)
        };
    }

    public static ApplicationError? ValidateProductTypeTrackingSaveDraft(SaveProductDraftRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ProductStructure) &&
            !ProductStructureConstants.TryNormalize(request.ProductStructure, out _))
        {
            return new ApplicationError(
                "product.invalid_product_structure",
                "Selected product structure is invalid. Allowed values: SIMPLE, VARIANT, BUNDLE.");
        }

        if (string.IsNullOrWhiteSpace(request.ProductStructure))
        {
            return null;
        }

        ProductStructureConstants.TryNormalize(request.ProductStructure, out var normalizedStructure);
        if (string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var (isValid, errorCode, errorMessage) = ProductTrackingRules.ValidateTrackingCombination(
            request.TrackInventory,
            request.BatchTracking,
            request.ExpiryTracking,
            request.SerialTracking);

        if (!isValid)
        {
            return new ApplicationError(errorCode!, errorMessage!);
        }

        return null;
    }

    public static ApplicationError? ValidateProductTypeTrackingContinue(SaveProductDraftRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductStructure) ||
            !ProductStructureConstants.TryNormalize(request.ProductStructure, out var normalizedStructure))
        {
            return new ApplicationError(
                "product.invalid_product_structure",
                "Selected product structure is invalid. Allowed values: SIMPLE, VARIANT, BUNDLE.");
        }

        if (string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var (isValid, errorCode, errorMessage) = ProductTrackingRules.ValidateTrackingCombination(
            request.TrackInventory,
            request.BatchTracking,
            request.ExpiryTracking,
            request.SerialTracking);

        if (!isValid)
        {
            return new ApplicationError(errorCode!, errorMessage!);
        }

        return null;
    }

    private static ApplicationError? ValidateStep1Draft(
        SaveProductDraftRequest request,
        bool requireProductName,
        bool requireCategory)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            if (requireProductName)
            {
                fieldErrors.Add(new ApplicationFieldError("productName", "Product name is required."));
            }
        }
        else if (requireProductName &&
                 ProductConstants.IsDraftProductNamePlaceholder(request.ProductName))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "productName",
                "Product name is required. Replace the draft placeholder before continuing."));
        }
        else if (request.ProductName.Trim().Length > ProductConstants.ProductNameMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "productName",
                $"Product name cannot exceed {ProductConstants.ProductNameMaxLength} characters."));
        }

        ValidateOptionalCode(fieldErrors, "productCode", request.ProductCode);
        ValidateOptionalCode(fieldErrors, "shortName", request.ShortName);

        if (request.ShortDescription is { Length: > 0 } &&
            request.ShortDescription.Trim().Length > ProductConstants.ShortDescriptionMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "shortDescription",
                $"Short description cannot exceed {ProductConstants.ShortDescriptionMaxLength} characters."));
        }

        if (request.LongDescription is { Length: > 0 } &&
            request.LongDescription.Trim().Length > ProductConstants.LongDescriptionMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "longDescription",
                $"Long description cannot exceed {ProductConstants.LongDescriptionMaxLength} characters."));
        }

        if (requireCategory && (!request.CategoryId.HasValue || request.CategoryId == Guid.Empty))
        {
            fieldErrors.Add(new ApplicationFieldError("categoryId", "Category is required."));
        }

        if (request.StagedMediaAssetIds is { Count: > 0 })
        {
            var distinctCount = request.StagedMediaAssetIds.Distinct().Count();
            if (distinctCount != request.StagedMediaAssetIds.Count)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "stagedMediaAssetIds",
                    "Staged media asset IDs must be distinct."));
            }

            if (distinctCount > ProductConstants.MaxProductImages)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "stagedMediaAssetIds",
                    $"At most {ProductConstants.MaxProductImages} staged media assets are allowed."));
            }
        }

        if (request.CurrentSetupStep is < 1 or > 7)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "currentSetupStep",
                "Current setup step must be between 1 and 7."));
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

    private static void ValidateOptionalCode(
        List<ApplicationFieldError> fieldErrors,
        string field,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ProductConstants.ProductCodeMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                field,
                $"{field} cannot exceed {ProductConstants.ProductCodeMaxLength} characters."));
        }
        else if (!AlphanumericDashRegex.IsMatch(trimmed))
        {
            fieldErrors.Add(new ApplicationFieldError(
                field,
                $"{field} may only contain letters, numbers, and dashes."));
        }
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

    public static ApplicationError? ValidateUnitsPackConversionDraft(SaveProductDraftRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (!string.IsNullOrWhiteSpace(request.UnitModel) &&
            !ProductUnitModelConstants.IsValid(request.UnitModel))
        {
            fieldErrors.Add(new ApplicationFieldError("unitModel", "Unit model must be SINGLE_UNIT or MULTIPLE_UNITS."));
        }

        if (request.ItemsPerPurchaseUnit.HasValue && request.ItemsPerPurchaseUnit.Value <= 0)
        {
            fieldErrors.Add(new ApplicationFieldError("itemsPerPurchaseUnit", "Items per purchase unit must be greater than zero."));
        }

        if (request.PurchaseUnitsPerOuterPack.HasValue && request.PurchaseUnitsPerOuterPack.Value <= 0)
        {
            fieldErrors.Add(new ApplicationFieldError("purchaseUnitsPerOuterPack", "Purchase units per outer pack must be greater than zero."));
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return new ApplicationError(
            "product.validation_failed",
            "Product draft validation failed.",
            fieldErrors);
    }

    public static ApplicationError? ValidateUnitsPackConversionContinue(SaveProductDraftRequest request)
    {
        var draftError = ValidateUnitsPackConversionDraft(request);
        if (draftError != null)
        {
            return draftError;
        }

        var fieldErrors = new List<ApplicationFieldError>();

        var normalizedModel = ProductUnitModelConstants.Normalize(request.UnitModel);

        if (string.Equals(normalizedModel, ProductUnitModelConstants.SingleUnit, StringComparison.OrdinalIgnoreCase))
        {
            var singleUnitId = request.ProductUnitId ?? request.BaseUnitId;
            if (!singleUnitId.HasValue || singleUnitId.Value == Guid.Empty)
            {
                fieldErrors.Add(new ApplicationFieldError("productUnitId", "Product Unit is required for Single Unit model."));
            }
        }
        else if (string.Equals(normalizedModel, ProductUnitModelConstants.MultipleUnits, StringComparison.OrdinalIgnoreCase))
        {
            if (!request.BaseUnitId.HasValue || request.BaseUnitId.Value == Guid.Empty)
            {
                fieldErrors.Add(new ApplicationFieldError("baseUnitId", "Base Unit is required for Multiple Units model."));
            }

            if (!request.SellingUnitId.HasValue || request.SellingUnitId.Value == Guid.Empty)
            {
                fieldErrors.Add(new ApplicationFieldError("sellingUnitId", "Selling Unit is required for Multiple Units model."));
            }

            if (!request.PurchaseUnitId.HasValue || request.PurchaseUnitId.Value == Guid.Empty)
            {
                fieldErrors.Add(new ApplicationFieldError("purchaseUnitId", "Purchase Unit is required for Multiple Units model."));
            }

            if (request.BaseUnitId.HasValue && request.PurchaseUnitId.HasValue && request.BaseUnitId.Value == request.PurchaseUnitId.Value)
            {
                fieldErrors.Add(new ApplicationFieldError("purchaseUnitId", "Purchase Unit must differ from Base Unit."));
            }

            if (!request.ItemsPerPurchaseUnit.HasValue || request.ItemsPerPurchaseUnit.Value <= 0)
            {
                fieldErrors.Add(new ApplicationFieldError("itemsPerPurchaseUnit", "Items per purchase unit is required and must be greater than zero."));
            }

            if (request.OuterPackUnitId.HasValue && request.OuterPackUnitId.Value != Guid.Empty)
            {
                if (request.BaseUnitId.HasValue && request.OuterPackUnitId.Value == request.BaseUnitId.Value)
                {
                    fieldErrors.Add(new ApplicationFieldError("outerPackUnitId", "Outer Pack Unit must differ from Base Unit."));
                }

                if (request.PurchaseUnitId.HasValue && request.OuterPackUnitId.Value == request.PurchaseUnitId.Value)
                {
                    fieldErrors.Add(new ApplicationFieldError("outerPackUnitId", "Outer Pack Unit must differ from Purchase Unit."));
                }

                if (!request.PurchaseUnitsPerOuterPack.HasValue || request.PurchaseUnitsPerOuterPack.Value <= 0)
                {
                    fieldErrors.Add(new ApplicationFieldError("purchaseUnitsPerOuterPack", "Purchase units per outer pack is required when Outer Pack Unit is selected."));
                }
            }

            // Selling Unit MUST match configured conversion tier (Base, Purchase, or Outer Pack)
            if (request.SellingUnitId.HasValue && request.SellingUnitId.Value != Guid.Empty)
            {
                var isBaseTier = request.BaseUnitId.HasValue && request.SellingUnitId.Value == request.BaseUnitId.Value;
                var isPurchaseTier = request.PurchaseUnitId.HasValue && request.SellingUnitId.Value == request.PurchaseUnitId.Value;
                var isOuterTier = request.OuterPackUnitId.HasValue && request.SellingUnitId.Value == request.OuterPackUnitId.Value;

                if (!isBaseTier && !isPurchaseTier && !isOuterTier)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        "sellingUnitId",
                        "Selling Unit must match Base Unit, Purchase Unit, or Outer Pack Unit.",
                        "unit.selling_unit_must_match_configured_tier"));
                }
            }

            // Integral Factor check when allowDecimalQuantity = false
            if (!request.AllowDecimalQuantity)
            {
                if (request.ItemsPerPurchaseUnit.HasValue && request.ItemsPerPurchaseUnit.Value % 1m != 0m)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        "allowDecimalQuantity",
                        "Items per Purchase Unit has a fractional part, which requires Decimal Quantity to be enabled.",
                        "unit.fractional_conversion_requires_decimal_quantity"));
                }

                if (request.ItemsPerPurchaseUnit.HasValue &&
                    request.PurchaseUnitsPerOuterPack.HasValue &&
                    (request.ItemsPerPurchaseUnit.Value * request.PurchaseUnitsPerOuterPack.Value) % 1m != 0m)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        "allowDecimalQuantity",
                        "Outer Pack conversion factor has a fractional part, which requires Decimal Quantity to be enabled.",
                        "unit.fractional_conversion_requires_decimal_quantity"));
                }
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

    public static ApplicationError? ValidateProductConfigurationDraft(SaveProductDraftRequest request)
    {
        ProductStructureConstants.TryNormalize(request.ProductStructure, out var normalizedStructure);

        if (string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateBundleConfigurationDraft(request);
        }
        
        if (string.Equals(normalizedStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateVariantConfigurationDraft(request);
        }

        // SIMPLE or others
        return null;
    }

    public static ApplicationError? ValidateProductConfigurationContinue(SaveProductDraftRequest request)
    {
        ProductStructureConstants.TryNormalize(request.ProductStructure, out var normalizedStructure);

        if (string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateBundleConfigurationContinue(request);
        }

        if (string.Equals(normalizedStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateVariantConfigurationContinue(request);
        }

        // SIMPLE or others
        return null;
    }

    private static ApplicationError? ValidateBundleConfigurationDraft(SaveProductDraftRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (request.VariantConfiguration != null)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "variantConfiguration",
                "Variant configuration is not applicable for a Bundle product.",
                "product.variant_configuration_not_applicable"));
        }

        if (request.BundleConfiguration != null && request.BundleConfiguration.Components != null)
        {
            for (int i = 0; i < request.BundleConfiguration.Components.Count; i++)
            {
                var component = request.BundleConfiguration.Components[i];

                if (component.ComponentProductId == Guid.Empty)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"bundleConfiguration.components[{i}].componentProductId",
                        "Component Product ID is required."));
                }

                if (component.ComponentUomId == Guid.Empty)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"bundleConfiguration.components[{i}].componentUomId",
                        "Component UOM ID is required."));
                }

                if (component.RequiredQuantity <= 0)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"bundleConfiguration.components[{i}].requiredQuantity",
                        "Required quantity must be greater than zero.",
                        "product.bundle.component_quantity_invalid"));
                }

                if (component.SortOrder < 0)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"bundleConfiguration.components[{i}].sortOrder",
                        "Sort order must be zero or greater."));
                }
            }
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product draft validation failed.",
            fieldErrors);
    }

    private static ApplicationError? ValidateBundleConfigurationContinue(SaveProductDraftRequest request)
    {
        var draftError = ValidateBundleConfigurationDraft(request);
        if (draftError != null) return draftError;

        var fieldErrors = new List<ApplicationFieldError>();

        if (request.BundleConfiguration == null || request.BundleConfiguration.Components == null || request.BundleConfiguration.Components.Count < 2)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "bundleConfiguration.components",
                "At least 2 components are required for a Bundle product.",
                "product.bundle.minimum_components_required"));
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product validation failed.",
            fieldErrors);
    }

    private static ApplicationError? ValidateVariantConfigurationDraft(SaveProductDraftRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (request.BundleConfiguration != null)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "bundleConfiguration",
                "Bundle configuration is not applicable for a Variant product.",
                "product.bundle_configuration_not_applicable"));
        }

        if (request.VariantConfiguration != null)
        {
            if (request.VariantConfiguration.Options != null)
            {
                var optionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < request.VariantConfiguration.Options.Count; i++)
                {
                    var option = request.VariantConfiguration.Options[i];
                    if (!string.IsNullOrWhiteSpace(option.OptionCode))
                    {
                        if (!optionCodes.Add(option.OptionCode))
                        {
                            fieldErrors.Add(new ApplicationFieldError(
                                $"options[{i}].optionCode",
                                $"Attribute '{option.OptionCode}' cannot be selected more than once.",
                                "product.duplicate_attribute"));
                        }
                    }

                    if (option.Values != null)
                    {
                        var valueCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for (int j = 0; j < option.Values.Count; j++)
                        {
                            var val = option.Values[j];
                            if (!string.IsNullOrWhiteSpace(val.ValueCode))
                            {
                                if (!valueCodes.Add(val.ValueCode))
                                {
                                    fieldErrors.Add(new ApplicationFieldError(
                                        $"options[{i}].values[{j}]",
                                        $"Value '{val.ValueCode}' cannot be repeated in attribute '{option.OptionCode}'.",
                                        "product.duplicate_option_value"));
                                }
                            }
                            
                            if (!val.SourceOptionTemplateValueId.HasValue && string.IsNullOrWhiteSpace(val.ValueName))
                            {
                                fieldErrors.Add(new ApplicationFieldError(
                                    $"options[{i}].values[{j}]",
                                    "Selected value must have a valid identifier or name."));
                            }
                        }
                    }
                }
            }

            if (request.VariantConfiguration.Variants != null)
            {
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < request.VariantConfiguration.Variants.Count; i++)
                {
                    var variant = request.VariantConfiguration.Variants[i];
                    
                    if (!string.IsNullOrWhiteSpace(variant.ClientCombinationKey))
                    {
                        if (!keys.Add(variant.ClientCombinationKey))
                        {
                            fieldErrors.Add(new ApplicationFieldError(
                                $"variants[{i}].clientCombinationKey",
                                "Duplicate clientCombinationKey detected."));
                        }
                    }
                    else
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variants[{i}].clientCombinationKey",
                            "clientCombinationKey is required."));
                    }

                    if (variant.DisplayLabel?.Length > 150)
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variants[{i}].displayLabel",
                            "Display label cannot exceed 150 characters."));
                    }
                }
            }
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product draft validation failed.",
            fieldErrors);
    }

    private static ApplicationError? ValidateVariantConfigurationContinue(SaveProductDraftRequest request)
    {
        var draftError = ValidateVariantConfigurationDraft(request);
        if (draftError != null) return draftError;

        var fieldErrors = new List<ApplicationFieldError>();

        if (request.VariantConfiguration == null || 
            request.VariantConfiguration.Options == null || 
            request.VariantConfiguration.Options.Count == 0)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "variantConfiguration.options",
                "At least one attribute must be defined for a Variant product.",
                "product.variant_options_required"));
        }
        else
        {
            long cartesianCount = 1;
            for (int i = 0; i < request.VariantConfiguration.Options.Count; i++)
            {
                var option = request.VariantConfiguration.Options[i];
                if (option.Values == null || option.Values.Count == 0)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"options[{i}].values",
                        $"Attribute '{option.OptionCode}' must contain at least one selected value.",
                        "product.option_values_required"));
                    cartesianCount = 0; 
                }
                else
                {
                    cartesianCount *= option.Values.Count;
                }
            }

            if (cartesianCount > ProductConstants.MaxVariants)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "variantConfiguration",
                    $"Cartesian matrix produces {cartesianCount} variants, exceeding maximum allowed limit of {ProductConstants.MaxVariants}.",
                    "product.max_variants_exceeded"));
            }
        }

        if (request.VariantConfiguration != null && request.VariantConfiguration.Variants != null)
        {
            bool hasIncluded = request.VariantConfiguration.Variants.Any(v => v.Included);
            if (!hasIncluded)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "variantConfiguration.variants",
                    "At least one variant must be included in the product setup.",
                    "product.included_variant_required"));
            }
            
            for (int i = 0; i < request.VariantConfiguration.Variants.Count; i++)
            {
                var variant = request.VariantConfiguration.Variants[i];
                if (variant.SelectedValues == null || variant.SelectedValues.Count == 0)
                {
                     fieldErrors.Add(new ApplicationFieldError(
                        $"variants[{i}].selectedValues",
                        "Variant must have selected option value identities."));
                }
            }
        }
        else
        {
            fieldErrors.Add(new ApplicationFieldError(
                "variantConfiguration.variants",
                "At least one variant must be included in the product setup.",
                "product.included_variant_required"));
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product validation failed.",
            fieldErrors);
    }

    public static ApplicationError? ValidateBarcodeSkuDraft(SaveProductDraftRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (request.BarcodeSkuConfiguration?.Assignments != null)
        {
            var skuSet = new HashSet<string>(StringComparer.Ordinal);
            var barcodeSet = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < request.BarcodeSkuConfiguration.Assignments.Count; i++)
            {
                var assignment = request.BarcodeSkuConfiguration.Assignments[i];
                var prefix = $"barcodeSkuConfiguration.assignments[{i}]";

                if (assignment.ProductVariantId == Guid.Empty)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.productVariantId", "Product variant identifier is required."));
                }

                if (assignment.Sku?.Length > 100)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.sku", "SKU cannot exceed 100 characters."));
                }
                else if (!string.IsNullOrWhiteSpace(assignment.Sku))
                {
                    if (!skuSet.Add(assignment.Sku.Trim()))
                    {
                        fieldErrors.Add(new ApplicationFieldError($"{prefix}.sku", "Duplicate SKU in request."));
                    }
                }

                if (assignment.Barcode?.Length > 100)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.barcode", "Barcode cannot exceed 100 characters."));
                }
                else if (!string.IsNullOrWhiteSpace(assignment.Barcode))
                {
                    if (!barcodeSet.Add(assignment.Barcode.Trim()))
                    {
                        fieldErrors.Add(new ApplicationFieldError($"{prefix}.barcode", "Duplicate barcode in request."));
                    }
                }
            }
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product draft validation failed.",
            fieldErrors);
    }

    public static ApplicationError? ValidateBarcodeSkuContinue(SaveProductDraftRequest request)
    {
        var draftError = ValidateBarcodeSkuDraft(request);
        if (draftError != null) return draftError;

        var fieldErrors = new List<ApplicationFieldError>();
        ProductStructureConstants.TryNormalize(request.ProductStructure, out var normalizedStructure);

        bool isVariant = string.Equals(normalizedStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase);

        if (request.BarcodeSkuConfiguration?.Assignments == null || request.BarcodeSkuConfiguration.Assignments.Count == 0)
        {
            fieldErrors.Add(new ApplicationFieldError("barcodeSkuConfiguration.assignments", "At least one SKU assignment is required."));
        }
        else
        {
            for (int i = 0; i < request.BarcodeSkuConfiguration.Assignments.Count; i++)
            {
                var assignment = request.BarcodeSkuConfiguration.Assignments[i];
                if (string.IsNullOrWhiteSpace(assignment.Sku))
                {
                    fieldErrors.Add(new ApplicationFieldError($"barcodeSkuConfiguration.assignments[{i}].sku", "SKU is required."));
                }
            }
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product validation failed.",
            fieldErrors);
    }

    public static ApplicationError? ValidatePricingTaxDraft(SaveProductDraftRequest request)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (request.PricingTax != null)
        {
            if (request.PricingTax.CostPrice.HasValue && request.PricingTax.CostPrice.Value < 0)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.costPrice", "Cost price cannot be negative."));
            }

            if (request.PricingTax.StandardSellingPrice.HasValue && request.PricingTax.StandardSellingPrice.Value < 0)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.standardSellingPrice", "Standard selling price cannot be negative."));
            }

            if (request.PricingTax.DiscountPrice.HasValue && request.PricingTax.DiscountPrice.Value < 0)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.discountPrice", "Discount price cannot be negative."));
            }

            if (request.PricingTax.DiscountPrice.HasValue && 
                request.PricingTax.StandardSellingPrice.HasValue && 
                request.PricingTax.DiscountPrice.Value >= request.PricingTax.StandardSellingPrice.Value)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.discountPrice", "Discount price must be less than standard selling price."));
            }

            if (request.PricingTax.TaxExclusive.HasValue && !request.PricingTax.TaxExclusive.Value)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.taxExclusive", "Pricing must be tax exclusive."));
            }
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product draft validation failed.",
            fieldErrors);
    }

    public static ApplicationError? ValidatePricingTaxContinue(SaveProductDraftRequest request)
    {
        var draftError = ValidatePricingTaxDraft(request);
        if (draftError != null) return draftError;

        var fieldErrors = new List<ApplicationFieldError>();

        if (request.PricingTax == null)
        {
            fieldErrors.Add(new ApplicationFieldError("pricingTax", "Pricing & Tax configuration is required."));
        }
        else
        {
            if (!request.PricingTax.CostPrice.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.costPrice", "Cost price is required."));
            }

            if (!request.PricingTax.StandardSellingPrice.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.standardSellingPrice", "Standard selling price is required."));
            }

            if (!request.PricingTax.TaxClassId.HasValue || request.PricingTax.TaxClassId.Value == Guid.Empty)
            {
                fieldErrors.Add(new ApplicationFieldError("pricingTax.taxClassId", "Tax class is required."));
            }
        }

        if (fieldErrors.Count == 0) return null;

        return new ApplicationError(
            "product.validation_failed",
            "Product validation failed.",
            fieldErrors);
    }
}

