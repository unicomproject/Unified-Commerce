using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed partial class TenantAdminProductRepository
{
    /// <summary>
    /// Atomic final Create for the 7-step Product Wizard (Chunk 6).
    /// Does not use the draft save pipeline or Draft status.
    /// </summary>
    public async Task<SaveProductDraftResult> CreateProductFromWizardAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminWizardProductCreateRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var structure = (request.ProductStructure ?? "SIMPLE").Trim().ToUpperInvariant();
            if (structure is not ("SIMPLE" or "VARIANT"))
            {
                return SaveProductDraftResult.Failure(new ApplicationError(
                    "product.validation_failed",
                    "Product structure must be SIMPLE or VARIANT."));
            }

            var channelIds = await ResolvePosAndOnlineChannelIdsAsync(tenantId, cancellationToken);
            if (channelIds.Error is not null)
            {
                return SaveProductDraftResult.Failure(channelIds.Error);
            }

            var productId = Guid.NewGuid();
            var (productCode, codeError) = await EnsureUniqueProductCodeAsync(
                tenantId,
                request.ProductCode,
                excludeProductId: null,
                cancellationToken);
            if (codeError is not null)
            {
                return SaveProductDraftResult.Failure(codeError);
            }

            var desiredStatus = request.DesiredPublishActive
                ? ProductConstants.ActiveStatus
                : ProductConstants.InactiveStatus;

            var slug = GenerateSlug(request.ProductName, productCode);
            var product = Product.Create(
                productId,
                tenantId,
                productCode,
                request.ProductName.Trim(),
                slug,
                ProductConstants.DefaultDraftProductType,
                structure,
                businessTypeId: null,
                request.BrandId,
                returnPolicyId: null,
                request.ShortDescription,
                request.LongDescription,
                isSellable: request.PosSellable || request.AllowOnlineSale,
                isTaxable: true,
                desiredStatus,
                userId,
                now,
                isExplicitDraftSave: false);

            // Mark as published immediately (no DRAFT intermediate).
            product.SetPublished(userId, now, desiredStatus);

            await _dbContext.Products.AddAsync(product, cancellationToken);

            if (request.CategoryId != Guid.Empty)
            {
                await _dbContext.ProductCategories.AddAsync(
                    ProductCategory.Create(
                        Guid.NewGuid(),
                        tenantId,
                        productId,
                        request.CategoryId,
                        isPrimaryCategory: true,
                        sortOrder: 0,
                        userId,
                        now),
                    cancellationToken);
            }

            await UpsertChannelVisibilityAsync(
                tenantId, productId, channelIds.PosSalesChannelId!.Value, request.PosSellable, userId, now, cancellationToken);
            await UpsertChannelVisibilityAsync(
                tenantId, productId, channelIds.OnlineSalesChannelId!.Value, request.AllowOnlineSale, userId, now, cancellationToken);

            var inventoryError = await UpsertInventorySettingAsync(
                tenantId,
                productId,
                request.TrackInventory,
                request.BatchTracking,
                request.ExpiryTracking,
                request.SerialTracking,
                userId,
                now,
                cancellationToken);
            if (inventoryError is not null)
            {
                return SaveProductDraftResult.Failure(inventoryError);
            }

            Guid? defaultUomId = null;
            if (structure == "SIMPLE")
            {
                var baseUnitId = request.BaseUnitId ?? request.ProductUnitId;
                defaultUomId = baseUnitId;

                var unitCommand = new SaveProductDraftCommand(
                    ProductId: productId,
                    ProductName: request.ProductName,
                    ProductCode: productCode,
                    ProductSlug: slug,
                    ProductStructure: structure,
                    CategoryId: request.CategoryId,
                    BrandId: request.BrandId,
                    ShortDescription: request.ShortDescription,
                    LongDescription: request.LongDescription,
                    DesiredPublishStatus: desiredStatus,
                    PosSellable: request.PosSellable,
                    TrackInventory: request.TrackInventory,
                    BatchTracking: request.BatchTracking,
                    ExpiryTracking: request.ExpiryTracking,
                    SerialTracking: request.SerialTracking,
                    AllowOnlineSale: request.AllowOnlineSale,
                    CurrentStage: 3,
                    TargetSetupStep: 7,
                    ExpectedRowVersion: null,
                    StagedMediaAssetIds: request.StagedMediaAssetIds ?? Array.Empty<Guid>(),
                    UnitModel: request.UnitModel,
                    BaseUnitId: baseUnitId,
                    SellingUnitId: request.SellingUnitId ?? baseUnitId,
                    PurchaseUnitId: request.PurchaseUnitId ?? baseUnitId,
                    OuterPackUnitId: request.OuterPackUnitId,
                    ItemsPerPurchaseUnit: request.ItemsPerPurchaseUnit,
                    PurchaseUnitsPerOuterPack: request.PurchaseUnitsPerOuterPack,
                    AllowDecimalQuantity: request.AllowDecimalQuantity,
                    IsExplicitDraftSave: false,
                    WizardAction: "CREATE_PRODUCT",
                    VariantConfiguration: null,
                    BundleConfiguration: null,
                    BarcodeSkuConfiguration: request.BarcodeSkuConfiguration,
                    PricingTax: request.PricingTax);

                var unitsError = await ApplyUnitsPackConversionAsync(
                    tenantId, userId, productId, unitCommand, now, cancellationToken);
                if (unitsError is not null)
                {
                    return SaveProductDraftResult.Failure(unitsError);
                }

                if (!defaultUomId.HasValue || defaultUomId.Value == Guid.Empty)
                {
                    defaultUomId = await GetDefaultInventoryUomIdAsync(tenantId, cancellationToken);
                }

                if (!defaultUomId.HasValue)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.validation_failed",
                        "A unit of measure is required for SIMPLE products."));
                }

                var simpleSku = request.BarcodeSkuConfiguration?.Assignments?
                    .FirstOrDefault(a =>
                        string.Equals(a.ClientCombinationKey, "SIMPLE_DEFAULT", StringComparison.OrdinalIgnoreCase) ||
                        a.ProductVariantId == null)
                    ?.Sku;

                if (string.IsNullOrWhiteSpace(simpleSku))
                {
                    simpleSku = productCode;
                }

                var simpleBarcode = request.BarcodeSkuConfiguration?.Assignments?
                    .FirstOrDefault(a =>
                        string.Equals(a.ClientCombinationKey, "SIMPLE_DEFAULT", StringComparison.OrdinalIgnoreCase) ||
                        a.ProductVariantId == null)
                    ?.Barcode;

                var defaultVariantId = Guid.NewGuid();
                var defaultVariant = ProductVariant.Create(
                    defaultVariantId,
                    tenantId,
                    productId,
                    "DEFAULT",
                    request.ProductName.Trim(),
                    simpleSku.Trim(),
                    defaultUomId.Value,
                    defaultUomId.Value,
                    isDefaultVariant: true,
                    isSellable: true,
                    allowFractionalQuantity: request.AllowDecimalQuantity,
                    desiredStatus,
                    userId,
                    now);
                await _dbContext.ProductVariants.AddAsync(defaultVariant, cancellationToken);

                if (!string.IsNullOrWhiteSpace(simpleBarcode))
                {
                    await _dbContext.ProductBarcodes.AddAsync(
                        ProductBarcode.Create(
                            Guid.NewGuid(),
                            tenantId,
                            productId,
                            defaultVariantId,
                            simpleBarcode.Trim(),
                            barcodeType: "EAN13",
                            uomId: null,
                            quantityPerScan: 1m,
                            isPrimaryBarcode: true,
                            status: ProductConstants.ActiveStatus,
                            userId,
                            now),
                        cancellationToken);
                }
            }
            else
            {
                // VARIANT
                if (request.VariantConfiguration is null ||
                    request.VariantConfiguration.Variants is null ||
                    request.VariantConfiguration.Variants.Count == 0)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.validation_failed",
                        "Variant configuration with at least one included variant is required."));
                }

                var included = request.VariantConfiguration.Variants
                    .Where(v => v.Included)
                    .ToList();
                if (included.Count == 0)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.validation_failed",
                        "At least one included variant is required."));
                }

                defaultUomId = await GetDefaultInventoryUomIdAsync(tenantId, cancellationToken);
                if (!defaultUomId.HasValue)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.validation_failed",
                        "No inventory unit of measure is available for this tenant."));
                }

                // Normalize: use ClientCombinationKey as OptionCombinationHash when hash missing.
                var normalizedVariants = request.VariantConfiguration.Variants
                    .Select(v => v with
                    {
                        OptionCombinationHash = string.IsNullOrWhiteSpace(v.OptionCombinationHash)
                            ? v.ClientCombinationKey
                            : v.OptionCombinationHash,
                        Status = desiredStatus,
                    })
                    .ToList();

                var normalizedConfig = request.VariantConfiguration with
                {
                    Variants = normalizedVariants,
                };

                await SaveVariantsAsync(tenantId, productId, normalizedConfig, cancellationToken);

                // Prefer Local for variants added in this transaction (not yet flushed).
                var createdVariants = _dbContext.ProductVariants.Local
                    .Where(v => v.TenantId == tenantId && v.ProductId == productId)
                    .ToList();
                if (createdVariants.Count == 0)
                {
                    createdVariants = await _dbContext.ProductVariants
                        .Where(v => v.TenantId == tenantId && v.ProductId == productId)
                        .ToListAsync(cancellationToken);
                }
                foreach (var variant in createdVariants)
                {
                    if (variant.StockUomId == Guid.Empty || variant.SalesUomId == Guid.Empty)
                    {
                        variant.UpdateUom(defaultUomId.Value, defaultUomId.Value, false, userId, now);
                    }
                }

                // Apply SKU/barcode by ClientCombinationKey → OptionCombinationHash
                var barcodeError = await ApplyWizardBarcodeSkuAsync(
                    tenantId,
                    userId,
                    productId,
                    request.BarcodeSkuConfiguration,
                    activateIdentifiers: true,
                    now,
                    cancellationToken);
                if (barcodeError is not null)
                {
                    return SaveProductDraftResult.Failure(barcodeError);
                }
            }

            if (request.PricingTax is not null)
            {
                var pricingError = await ApplyPricingTaxConfigurationAsync(
                    tenantId, userId, product, request.PricingTax, now, cancellationToken);
                if (pricingError is not null)
                {
                    return SaveProductDraftResult.Failure(pricingError);
                }
            }

            if (request.StagedMediaAssetIds is { Count: > 0 })
            {
                var mediaError = await LinkStagedMediaAsync(
                    tenantId, productId, request.StagedMediaAssetIds, userId, now, cancellationToken);
                if (mediaError is not null)
                {
                    return SaveProductDraftResult.Failure(mediaError);
                }
            }

            var trackingRow = await UpsertInitialTrackingAsync(
                tenantId,
                productId,
                userId,
                request.InitialBatchNumber,
                request.InitialExpiryDate,
                request.InitialSerialNumber,
                request.InitialTrackingAssignedVariantId,
                request.ConfirmClearIncompatibleInitialTracking,
                now,
                cancellationToken);

            var identityError = await PublishInitialTrackingIdentityAsync(
                tenantId,
                productId,
                userId,
                structure,
                request.TrackInventory,
                request.BatchTracking,
                request.ExpiryTracking,
                request.SerialTracking,
                trackingRow.InitialBatchNumber,
                trackingRow.InitialExpiryDate,
                trackingRow.InitialSerialNumber,
                trackingRow.AssignedProductVariantId,
                now,
                cancellationToken);
            if (identityError is not null)
            {
                return SaveProductDraftResult.Failure(identityError);
            }

            await _dbContext.AuditLogs.AddAsync(new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = userId,
                ActorType = "TENANT_USER",
                EntityType = "PRODUCT",
                EntityId = product.Id,
                Action = "PRODUCT_CREATED",
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    productStructure = structure,
                    source = "wizard-create",
                    productName = product.ProductName,
                    productCode = product.ProductCode,
                }),
                CreatedAt = now,
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var setup = await GetSetupAsync(tenantId, productId, cancellationToken);
            if (setup is null)
            {
                return SaveProductDraftResult.Success(new ProductDraftResponse(
                    productId,
                    product.ProductName,
                    product.ProductCode,
                    product.Status,
                    desiredStatus,
                    7,
                    null,
                    product.RowVersion,
                    request.CategoryId,
                    request.BrandId,
                    request.ShortDescription,
                    request.LongDescription,
                    request.PosSellable,
                    request.TrackInventory,
                    request.BatchTracking,
                    request.ExpiryTracking,
                    request.SerialTracking,
                    structure,
                    request.AllowOnlineSale,
                    Array.Empty<TenantAdminProductImageResponse>()));
            }

            return SaveProductDraftResult.Success(MapSetupToDraftResponse(setup));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ApplicationError?> ApplyWizardBarcodeSkuAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        BarcodeSkuConfigurationDto? configuration,
        bool activateIdentifiers,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (configuration?.Assignments is null || configuration.Assignments.Count == 0)
        {
            return new ApplicationError(
                "product.validation_failed",
                "SKU assignments are required for every active variant.");
        }

        var variants = _dbContext.ProductVariants.Local
            .Where(v => v.TenantId == tenantId && v.ProductId == productId)
            .ToList();
        if (variants.Count == 0)
        {
            variants = await _dbContext.ProductVariants
                .Where(v => v.TenantId == tenantId && v.ProductId == productId)
                .ToListAsync(cancellationToken);
        }

        var included = variants.Where(v => v.IsSellable).ToList();
        if (included.Count == 0)
        {
            included = variants;
        }

        var existingBarcodes = await _dbContext.ProductBarcodes
            .Where(b => b.TenantId == tenantId && b.ProductId == productId)
            .ToListAsync(cancellationToken);

        var barcodeStatus = activateIdentifiers
            ? ProductConstants.ActiveStatus
            : ProductConstants.InactiveStatus;

        var assignedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assignment in configuration.Assignments)
        {
            ProductVariant? target = null;
            if (assignment.ProductVariantId.HasValue)
            {
                target = variants.FirstOrDefault(v => v.Id == assignment.ProductVariantId.Value);
            }

            if (target is null && !string.IsNullOrWhiteSpace(assignment.ClientCombinationKey))
            {
                var key = assignment.ClientCombinationKey.Trim();
                target = variants.FirstOrDefault(v =>
                    string.Equals(v.OptionCombinationHash?.Trim(), key, StringComparison.Ordinal));
            }

            if (target is null)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    $"No variant found for identifier assignment '{assignment.ClientCombinationKey ?? assignment.DisplayName}'.");
            }

            if (string.IsNullOrWhiteSpace(assignment.Sku))
            {
                return new ApplicationError(
                    "product.validation_failed",
                    $"SKU is required for variant '{target.VariantName}'.");
            }

            target.UpdateSku(assignment.Sku.Trim(), userId, now);
            assignedKeys.Add(target.OptionCombinationHash ?? target.Id.ToString());

            var existingBarcode = existingBarcodes.FirstOrDefault(b => b.ProductVariantId == target.Id);
            if (!string.IsNullOrWhiteSpace(assignment.Barcode))
            {
                if (existingBarcode is null)
                {
                    await _dbContext.ProductBarcodes.AddAsync(
                        ProductBarcode.Create(
                            Guid.NewGuid(),
                            tenantId,
                            productId,
                            target.Id,
                            assignment.Barcode.Trim(),
                            "EAN13",
                            null,
                            1m,
                            true,
                            barcodeStatus,
                            userId,
                            now),
                        cancellationToken);
                }
                else
                {
                    existingBarcode.UpdateIdentifier(assignment.Barcode.Trim(), existingBarcode.BarcodeType, userId, now);
                }
            }
        }

        foreach (var variant in included)
        {
            var key = variant.OptionCombinationHash ?? variant.Id.ToString();
            if (!assignedKeys.Contains(key) && string.IsNullOrWhiteSpace(variant.Sku))
            {
                return new ApplicationError(
                    "product.validation_failed",
                    $"SKU is required for variant '{variant.VariantName}'.");
            }
        }

        return null;
    }

    private static ProductDraftResponse MapSetupToDraftResponse(ProductSetupWizardDto setup)
    {
        return new ProductDraftResponse(
            setup.ProductId,
            setup.ProductName,
            setup.ProductCode,
            setup.Status,
            setup.DesiredPublishStatus,
            setup.CurrentSetupStep,
            setup.DraftSavedAt,
            setup.RowVersion,
            setup.CategoryId,
            setup.BrandId,
            setup.ShortDescription,
            setup.LongDescription,
            setup.PosSellable,
            setup.TrackInventory,
            setup.BatchTracking,
            setup.ExpiryTracking,
            setup.SerialTracking,
            setup.ProductStructure,
            setup.AllowOnlineSale,
            setup.Images,
            setup.CategoryName,
            setup.BrandName,
            setup.CreatedByTenantUserId,
            setup.CreatedByName,
            setup.CreatedAt,
            setup.Sku,
            setup.PrimaryImageUrl,
            setup.InventoryMethod,
            setup.ComponentCount,
            setup.ComponentsConfigured,
            setup.TargetSetupStep,
            setup.LastCompletedSetupStep,
            setup.UnitModel,
            setup.BaseUnitId,
            setup.BaseUnitName,
            setup.SellingUnitId,
            setup.SellingUnitName,
            setup.PurchaseUnitId,
            setup.PurchaseUnitName,
            setup.OuterPackUnitId,
            setup.OuterPackUnitName,
            setup.ItemsPerPurchaseUnit,
            setup.PurchaseUnitsPerOuterPack,
            setup.AllowDecimalQuantity,
            setup.UnitConversions,
            setup.VariantConfiguration,
            setup.BundleConfiguration,
            setup.BarcodeSkuConfiguration,
            setup.PricingTax,
            setup.TotalVariantCount,
            setup.IncludedVariantCount,
            setup.InitialBatchNumber,
            setup.InitialExpiryDate,
            setup.InitialSerialNumber,
            setup.InitialTrackingAssignedVariantId);
    }
}
