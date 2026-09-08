using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Application.Modules.Tenant.PricingTax.Services;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed partial class TenantAdminProductRepository
{
    private const string DefaultCostingMethod = "WEIGHTED_AVERAGE";
    private const string StagedMediaStatus = "STAGED";

    public async Task<bool> IsCategoryEffectivelySelectableAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != CategoryConstants.DeletedStatus)
            .Select(x => new { x.Id, x.Status, x.ParentCategoryId })
            .ToListAsync(cancellationToken);

        var statusById = rows.ToDictionary(x => x.Id, x => x.Status);
        var parentById = rows.ToDictionary(x => x.Id, x => x.ParentCategoryId);

        return CategorySelectionRules.IsEffectivelySelectable(categoryId, statusById, parentById);
    }

    public Task<bool> ActiveCategoryExistsAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken) =>
        IsCategoryEffectivelySelectableAsync(tenantId, categoryId, cancellationToken);

    public Task<bool> CategoryExistsForExistingMappingAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == categoryId &&
                     x.Status != CategoryConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<bool> ProductCodeExistsAsync(
        Guid tenantId,
        string productCode,
        Guid? excludeProductId,
        CancellationToken cancellationToken)
    {
        var normalized = ProductConstants.NormalizeCode(productCode);
        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProductCode == normalized &&
                     x.Status != ProductConstants.ArchivedStatus &&
                     (!excludeProductId.HasValue || x.Id != excludeProductId.Value),
                cancellationToken);
    }

    public async Task<Guid?> GetDefaultInventoryUomIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var preferredCodes = new[] { "PIECE", "EACH" };

        var preferred = await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x =>
                (x.TenantId == null || x.TenantId == tenantId) &&
                x.Status != "DELETED" &&
                preferredCodes.Contains(x.UomCode.ToUpper()))
            .OrderBy(x => x.TenantId == null ? 0 : 1)
            .ThenBy(x => x.UomCode)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (preferred.HasValue)
        {
            return preferred;
        }

        return await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x =>
                (x.TenantId == null || x.TenantId == tenantId) &&
                x.Status != "DELETED")
            .OrderBy(x => x.TenantId == null ? 0 : 1)
            .ThenBy(x => x.UomCode)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasOperationalHistoryAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var hasStockMovements = await (
            from sm in _dbContext.StockMovements.AsNoTracking()
            join ib in _dbContext.InventoryBalances.AsNoTracking()
                on sm.InventoryBalanceId equals ib.Id
            where sm.TenantId == tenantId && ib.ProductId == productId
            select sm.Id)
            .AnyAsync(cancellationToken);

        if (hasStockMovements)
        {
            return true;
        }

        var hasOrders = await _dbContext.SalesOrderLines
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.ProductId == productId, cancellationToken);

        return hasOrders;
    }

    public async Task<string?> GetTenantStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsInitialCreationDraftAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == productId, cancellationToken);

        return product != null && product.Status == ProductConstants.DraftStatus && product.PublishedAt == null;
    }

    public async Task<SaveProductDraftResult> SaveProductDraftAsync(
        Guid tenantId,
        Guid userId,
        SaveProductDraftCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var channelIds = await ResolvePosAndOnlineChannelIdsAsync(tenantId, cancellationToken);
            if (channelIds.Error is not null)
            {
                return SaveProductDraftResult.Failure(channelIds.Error);
            }

            Product? product;
            var isCreate = !command.ProductId.HasValue;
            var oldStructure = string.Empty;
            var oldTrackInventory = false;
            var oldBatchTracking = false;
            var oldExpiryTracking = false;
            var oldSerialTracking = false;

            if (command.CurrentStage == ProductWizardStage.BasicDetails)
            {
                if (isCreate)
                {
                    var productId = Guid.NewGuid();
                    var (productCode, codeError) = await EnsureUniqueProductCodeAsync(
                        tenantId,
                        command.ProductCode,
                        excludeProductId: null,
                        cancellationToken);

                    if (codeError is not null)
                    {
                        return SaveProductDraftResult.Failure(codeError);
                    }

                    product = Product.Create(
                        productId,
                        tenantId,
                        productCode,
                        command.ProductName,
                        command.ProductSlug,
                        ProductConstants.DefaultDraftProductType,
                        string.IsNullOrWhiteSpace(command.ProductStructure)
                            ? ProductConstants.DefaultDraftProductStructure
                            : command.ProductStructure,
                        businessTypeId: null,
                        command.BrandId,
                        returnPolicyId: null,
                        command.ShortDescription,
                        command.LongDescription,
                        isSellable: command.PosSellable || command.AllowOnlineSale,
                        isTaxable: true,
                        ProductConstants.DraftStatus,
                        userId,
                        now,
                        command.IsExplicitDraftSave);

                    product.SaveWizardDraft(
                        command.TargetSetupStep,
                        command.DesiredPublishStatus,
                        userId,
                        now,
                        command.IsExplicitDraftSave);

                    await _dbContext.Products.AddAsync(product, cancellationToken);
                }
                else
                {
                    if (!command.ExpectedRowVersion.HasValue)
                    {
                        return SaveProductDraftResult.Failure(new ApplicationError(
                            "product.row_version_required",
                            "expectedRowVersion is required when updating a persisted product."));
                    }

                    product = await _dbContext.Products
                        .FirstOrDefaultAsync(
                            x => x.TenantId == tenantId &&
                                 x.Id == command.ProductId!.Value &&
                                 x.Status != ProductConstants.ArchivedStatus,
                            cancellationToken);

                    if (product is null)
                    {
                        return SaveProductDraftResult.Failure(new ApplicationError(
                            "product.not_found",
                            "Product was not found."));
                    }

                    if (product.RowVersion != command.ExpectedRowVersion.Value)
                    {
                        return SaveProductDraftResult.Failure(new ApplicationError(
                            "product.concurrency_conflict",
                            "Product was modified by another user. Refresh and try again."));
                    }

                    oldStructure = product.ProductStructure;
                    var oldSetting = await _dbContext.ProductInventorySettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == product.Id && x.ProductVariantId == null, cancellationToken);

                    if (oldSetting != null)
                    {
                        oldTrackInventory = oldSetting.IsStockTracked;
                        oldBatchTracking = oldSetting.RequiresBatchTracking;
                        oldExpiryTracking = oldSetting.RequiresExpiryTracking;
                        oldSerialTracking = oldSetting.RequiresSerialTracking;
                    }

                    var (productCode, codeError) = await EnsureUniqueProductCodeAsync(
                        tenantId,
                        string.IsNullOrWhiteSpace(command.ProductCode)
                            ? product.ProductCode
                            : command.ProductCode,
                        product.Id,
                        cancellationToken);

                    if (codeError is not null)
                    {
                        return SaveProductDraftResult.Failure(codeError);
                    }

                    product.UpdateWizardStep1Profile(
                        productCode,
                        command.ProductName,
                        command.ProductSlug,
                        command.BrandId,
                        command.ShortDescription,
                        command.LongDescription,
                        isSellable: command.PosSellable || command.AllowOnlineSale,
                        userId,
                        now);

                    if (!string.IsNullOrWhiteSpace(command.ProductStructure))
                    {
                        product.UpdateWizardStep2Profile(
                            command.ProductStructure,
                            userId,
                            now);
                    }

                    product.SaveWizardDraft(
                        command.TargetSetupStep,
                        command.DesiredPublishStatus,
                        userId,
                        now,
                        command.IsExplicitDraftSave);
                }

                if (command.CategoryId.HasValue)
                {
                    await UpsertPrimaryCategoryAsync(
                        tenantId,
                        product.Id,
                        command.CategoryId.Value,
                        userId,
                        now,
                        cancellationToken);
                }
                else
                {
                    await ClearProductCategoriesAsync(tenantId, product.Id, cancellationToken);
                }

                await UpsertChannelVisibilityAsync(
                    tenantId,
                    product.Id,
                    channelIds.PosSalesChannelId!.Value,
                    command.PosSellable,
                    userId,
                    now,
                    cancellationToken);

                await UpsertChannelVisibilityAsync(
                    tenantId,
                    product.Id,
                    channelIds.OnlineSalesChannelId!.Value,
                    command.AllowOnlineSale,
                    userId,
                    now,
                    cancellationToken);

                var mediaError = await LinkStagedMediaAsync(
                    tenantId,
                    product.Id,
                    command.StagedMediaAssetIds,
                    userId,
                    now,
                    cancellationToken);
                if (mediaError is not null)
                {
                    return SaveProductDraftResult.Failure(mediaError);
                }
            }
            else if (command.CurrentStage == ProductWizardStage.ProductTypeTracking)
            {
                if (!command.ExpectedRowVersion.HasValue)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.row_version_required",
                        "expectedRowVersion is required when updating a persisted product."));
                }

                product = await _dbContext.Products
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == command.ProductId!.Value &&
                             x.Status != ProductConstants.ArchivedStatus,
                        cancellationToken);

                if (product is null)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.not_found",
                        "Product was not found."));
                }

                if (product.RowVersion != command.ExpectedRowVersion.Value)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.concurrency_conflict",
                        "Product was modified by another user. Refresh and try again."));
                }

                oldStructure = product.ProductStructure;
                var oldSetting = await _dbContext.ProductInventorySettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == product.Id && x.ProductVariantId == null, cancellationToken);

                if (oldSetting != null)
                {
                    oldTrackInventory = oldSetting.IsStockTracked;
                    oldBatchTracking = oldSetting.RequiresBatchTracking;
                    oldExpiryTracking = oldSetting.RequiresExpiryTracking;
                    oldSerialTracking = oldSetting.RequiresSerialTracking;
                }

                if (!string.IsNullOrWhiteSpace(command.ProductStructure))
                {
                    product.UpdateWizardStep2Profile(
                        command.ProductStructure,
                        userId,
                        now);
                }

                product.SaveWizardDraft(
                    command.TargetSetupStep,
                    command.DesiredPublishStatus,
                    userId,
                    now);
            }
            else if (command.CurrentStage == ProductWizardStage.UnitsPackConversion)
            {
                if (!command.ExpectedRowVersion.HasValue)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.row_version_required",
                        "expectedRowVersion is required when updating a persisted product."));
                }

                product = await _dbContext.Products
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == command.ProductId!.Value &&
                             x.Status != ProductConstants.ArchivedStatus,
                        cancellationToken);

                if (product is null)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.not_found",
                        "Product was not found."));
                }

                if (product.RowVersion != command.ExpectedRowVersion.Value)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.concurrency_conflict",
                        "Product was modified by another user. Refresh and try again."));
                }

                oldStructure = product.ProductStructure;
                var oldSetting = await _dbContext.ProductInventorySettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == product.Id && x.ProductVariantId == null, cancellationToken);

                if (oldSetting != null)
                {
                    oldTrackInventory = oldSetting.IsStockTracked;
                    oldBatchTracking = oldSetting.RequiresBatchTracking;
                    oldExpiryTracking = oldSetting.RequiresExpiryTracking;
                    oldSerialTracking = oldSetting.RequiresSerialTracking;
                }

                var unitError = await ApplyUnitsPackConversionAsync(
                    tenantId,
                    userId,
                    product.Id,
                    command,
                    now,
                    cancellationToken);

                if (unitError is not null)
                {
                    return SaveProductDraftResult.Failure(unitError);
                }

                product.SaveWizardDraft(
                    command.TargetSetupStep,
                    command.DesiredPublishStatus,
                    userId,
                    now);
            }
            else if (command.CurrentStage == ProductWizardStage.BarcodeSku)
            {
                product = await _dbContext.Products
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == command.ProductId!.Value &&
                             x.Status != ProductConstants.ArchivedStatus,
                        cancellationToken);

                if (product is null)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.not_found",
                        "Product was not found."));
                }

                if (command.BarcodeSkuConfiguration != null)
                {
                    var barcodeError = await ApplyBarcodeSkuConfigurationAsync(
                        tenantId,
                        userId,
                        product.Id,
                        command.BarcodeSkuConfiguration,
                        now,
                        cancellationToken);

                    if (barcodeError is not null)
                    {
                        return SaveProductDraftResult.Failure(barcodeError);
                    }
                }

                product.SaveWizardDraft(
                    command.TargetSetupStep,
                    command.DesiredPublishStatus,
                    userId,
                    now);
            }
            else if (command.CurrentStage == ProductWizardStage.ProductConfiguration)
            {
                if (!command.ExpectedRowVersion.HasValue)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.row_version_required",
                        "expectedRowVersion is required when updating a persisted product."));
                }

                product = await _dbContext.Products
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == command.ProductId!.Value &&
                             x.Status != ProductConstants.ArchivedStatus,
                        cancellationToken);

                if (product is null)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.not_found",
                        "Product was not found."));
                }

                if (command.VariantConfiguration != null && string.Equals(product.ProductStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
                {
                    if (command.VariantConfiguration.Options.Any() && !command.VariantConfiguration.Variants.Any())
                    {
                        return SaveProductDraftResult.Failure(new ApplicationError(
                            "product.validation_failed",
                            "Variant combinations are required when variant options are defined.",
                            [new ApplicationFieldError("variants", "At least one variant must be configured.")]));
                    }

                    await SaveVariantsAsync(tenantId, product.Id, command.VariantConfiguration, cancellationToken);
                }

                product.SaveWizardDraft(
                    command.TargetSetupStep,
                    command.DesiredPublishStatus,
                    userId,
                    now,
                    command.IsExplicitDraftSave);
            }
            else if (command.CurrentStage == ProductWizardStage.PricingTax)
            {
                product = await _dbContext.Products
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == command.ProductId!.Value &&
                             x.Status != ProductConstants.ArchivedStatus,
                        cancellationToken);

                if (product is null)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.not_found",
                        "Product was not found."));
                }

                if (command.PricingTax != null)
                {
                    var pricingError = await ApplyPricingTaxConfigurationAsync(
                        tenantId,
                        userId,
                        product,
                        command.PricingTax,
                        now,
                        cancellationToken);

                    if (pricingError is not null)
                    {
                        return SaveProductDraftResult.Failure(pricingError);
                    }
                }

                product.SaveWizardDraft(
                    command.TargetSetupStep,
                    command.DesiredPublishStatus,
                    userId,
                    now);
            }
            else
            {
                product = await _dbContext.Products
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == command.ProductId!.Value &&
                             x.Status != ProductConstants.ArchivedStatus,
                        cancellationToken);

                if (product is null)
                {
                    return SaveProductDraftResult.Failure(new ApplicationError(
                        "product.not_found",
                        "Product was not found."));
                }

                product.SaveWizardDraft(
                    command.TargetSetupStep,
                    command.DesiredPublishStatus,
                    userId,
                    now);
            }

            var normalizedStructure = ProductStructureConstants.Normalize(command.ProductStructure);
            if (!string.IsNullOrWhiteSpace(oldStructure) &&
                !string.IsNullOrWhiteSpace(normalizedStructure) &&
                !string.Equals(oldStructure, normalizedStructure, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(oldStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
                {
                    var variantIds = await _dbContext.ProductVariants
                        .Where(x => x.TenantId == tenantId && x.ProductId == product.Id)
                        .Select(x => x.Id)
                        .ToListAsync(cancellationToken);

                    if (variantIds.Count > 0)
                    {
                        await SoftDeletePricingGraphForProductVariantsAsync(
                            tenantId,
                            product.Id,
                            variantIds,
                            userId,
                            now,
                            cancellationToken);

                        var variantOptionValues = _dbContext.ProductVariantOptionValues
                            .Where(x => x.TenantId == tenantId && variantIds.Contains(x.ProductVariantId));
                        _dbContext.ProductVariantOptionValues.RemoveRange(variantOptionValues);

                        var variantSettings = _dbContext.ProductInventorySettings
                            .Where(x => x.TenantId == tenantId && x.ProductVariantId.HasValue && variantIds.Contains(x.ProductVariantId.Value));
                        _dbContext.ProductInventorySettings.RemoveRange(variantSettings);

                        var variants = _dbContext.ProductVariants
                            .Where(x => x.TenantId == tenantId && x.ProductId == product.Id);
                        _dbContext.ProductVariants.RemoveRange(variants);
                    }
                }

                if (string.Equals(oldStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase))
                {
                    var comboDefs = await _dbContext.ComboDefinitions
                        .Where(x => x.TenantId == tenantId && x.ProductId == product.Id)
                        .Select(x => x.Id)
                        .ToListAsync(cancellationToken);

                    if (comboDefs.Count > 0)
                    {
                        var comboComponents = _dbContext.ComboComponents
                            .Where(x => x.TenantId == tenantId && comboDefs.Contains(x.ComboDefinitionId));
                        _dbContext.ComboComponents.RemoveRange(comboComponents);

                        var comboDefinitions = _dbContext.ComboDefinitions
                            .Where(x => x.TenantId == tenantId && x.ProductId == product.Id);
                        _dbContext.ComboDefinitions.RemoveRange(comboDefinitions);
                    }
                }
            }

            if (command.CurrentStage == ProductWizardStage.ProductTypeTracking || command.CurrentStage == ProductWizardStage.BasicDetails)
            {
                var isBundle = string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase);
                var trackStock = isBundle ? false : command.TrackInventory;
                var reqBatch = isBundle ? false : command.BatchTracking;
                var reqExpiry = isBundle ? false : command.ExpiryTracking;
                var reqSerial = isBundle ? false : command.SerialTracking;

                var inventoryError = await UpsertInventorySettingAsync(
                    tenantId,
                    product.Id,
                    trackStock,
                    reqBatch,
                    reqExpiry,
                    reqSerial,
                    userId,
                    now,
                    cancellationToken);

                if (inventoryError is not null)
                {
                    return SaveProductDraftResult.Failure(inventoryError);
                }
            }

            var trackingUpsert = await UpsertInitialTrackingAsync(
                tenantId,
                product.Id,
                userId,
                command.InitialBatchNumber,
                command.InitialExpiryDate,
                command.InitialSerialNumber,
                command.InitialTrackingAssignedVariantId,
                command.ConfirmClearIncompatibleInitialTracking,
                now,
                cancellationToken);

            if (command.CurrentStage == 7 && !command.IsExplicitDraftSave)
            {
                var publishPricingError = await ValidatePublishPricingAsync(
                    tenantId,
                    product,
                    cancellationToken);
                if (publishPricingError is not null)
                {
                    return SaveProductDraftResult.Failure(publishPricingError);
                }

                var identityError = await PublishInitialTrackingIdentityAsync(
                    tenantId,
                    product.Id,
                    userId,
                    normalizedStructure,
                    command.TrackInventory,
                    command.BatchTracking,
                    command.ExpiryTracking,
                    command.SerialTracking,
                    trackingUpsert.InitialBatchNumber,
                    trackingUpsert.InitialExpiryDate,
                    trackingUpsert.InitialSerialNumber,
                    trackingUpsert.AssignedProductVariantId,
                    now,
                    cancellationToken);
                if (identityError is not null)
                {
                    return SaveProductDraftResult.Failure(identityError);
                }

                product.SetPublished(userId, now, command.DesiredPublishStatus);
            }

            var auditAction = command.CurrentStage == 7 && !command.IsExplicitDraftSave
                ? "PRODUCT_CREATED"
                : (command.CurrentStage == ProductWizardStage.ProductTypeTracking
                    ? "PRODUCT_TYPE_TRACKING_SAVED"
                    : $"PRODUCT_DRAFT_STEP{command.CurrentStage}_SAVED");

            var auditLog = new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = userId,
                ActorType = "TENANT_USER",
                EntityType = command.CurrentStage == ProductWizardStage.ProductTypeTracking ? "PRODUCT_TYPE_TRACKING" : $"PRODUCT_DRAFT_STEP{command.CurrentStage}",
                EntityId = product.Id,
                Action = auditAction,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    productStructure = oldStructure,
                    trackInventory = oldTrackInventory,
                    batchTracking = oldBatchTracking,
                    expiryTracking = oldExpiryTracking,
                    serialTracking = oldSerialTracking
                }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    productStructure = normalizedStructure,
                    trackInventory = string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) ? false : command.TrackInventory,
                    batchTracking = string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) ? false : command.BatchTracking,
                    expiryTracking = string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) ? false : command.ExpiryTracking,
                    serialTracking = string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) ? false : command.SerialTracking
                }),
                CreatedAt = now
            };
            await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var images = await ProjectProductImagesAsync(tenantId, product.Id, cancellationToken);
            var categoryId = await GetPrimaryCategoryIdAsync(tenantId, product.Id, cancellationToken);
            var trackingFlags = await GetInventoryTrackingFlagsAsync(tenantId, product.Id, cancellationToken);
            var channelFlags = await GetChannelFlagsAsync(
                tenantId,
                product.Id,
                channelIds.PosSalesChannelId!.Value,
                channelIds.OnlineSalesChannelId!.Value,
                cancellationToken);

            var categoryName = categoryId.HasValue
                ? await _dbContext.Categories
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.Id == categoryId.Value)
                    .Select(x => x.CategoryName)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var brandName = product.BrandId.HasValue
                ? await _dbContext.Brands
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.Id == product.BrandId.Value)
                    .Select(x => x.BrandName)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var createdByName = product.CreatedByTenantUserId.HasValue
                ? await _dbContext.TenantUsers
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.Id == product.CreatedByTenantUserId.Value)
                    .Select(x => x.DisplayName ?? x.FullName ?? x.Email)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            var primaryImageUrl = images.FirstOrDefault()?.ImageUrl;

            var inventoryMethod = product.ProductStructure switch
            {
                ProductStructureConstants.Variant => "VARIANT_BASED",
                ProductStructureConstants.Bundle => "COMPONENT_BASED",
                _ => "PRODUCT_BASED"
            };

            var componentCount = await _dbContext.ComboDefinitions
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.ProductId == product.Id)
                .SelectMany(x => _dbContext.ComboComponents.Where(c => c.TenantId == tenantId && c.ComboDefinitionId == x.Id))
                .CountAsync(cancellationToken);

            var componentsConfigured = componentCount >= 2;
            var unitProjection = await ProjectProductUnitSettingsAsync(tenantId, product.Id, cancellationToken);
            var trackingValues = await LoadInitialTrackingValuesAsync(tenantId, product.Id, cancellationToken);

            return SaveProductDraftResult.Success(new ProductDraftResponse(
                product.Id,
                product.ProductName,
                product.ProductCode,
                product.Status,
                product.DesiredPublishStatus,
                product.CurrentSetupStep,
                product.DraftSavedAt,
                product.RowVersion,
                categoryId,
                product.BrandId,
                product.ShortDescription,
                product.LongDescription,
                channelFlags.PosSellable,
                trackingFlags.TrackInventory,
                trackingFlags.BatchTracking,
                trackingFlags.ExpiryTracking,
                trackingFlags.SerialTracking,
                product.ProductStructure,
                channelFlags.AllowOnlineSale,
                images,
                CategoryName: categoryName,
                BrandName: brandName,
                CreatedByTenantUserId: product.CreatedByTenantUserId,
                CreatedByName: createdByName,
                CreatedAt: product.CreatedAt,
                Sku: null,
                PrimaryImageUrl: primaryImageUrl,
                InventoryMethod: inventoryMethod,
                ComponentCount: componentCount,
                ComponentsConfigured: componentsConfigured,
                TargetSetupStep: command.TargetSetupStep,
                LastCompletedSetupStep: product.CurrentSetupStep,
                UnitModel: unitProjection.UnitModel,
                BaseUnitId: unitProjection.BaseUnitId,
                BaseUnitName: unitProjection.BaseUnitName,
                SellingUnitId: unitProjection.SellingUnitId,
                SellingUnitName: unitProjection.SellingUnitName,
                PurchaseUnitId: unitProjection.PurchaseUnitId,
                PurchaseUnitName: unitProjection.PurchaseUnitName,
                OuterPackUnitId: unitProjection.OuterPackUnitId,
                OuterPackUnitName: unitProjection.OuterPackUnitName,
                ItemsPerPurchaseUnit: unitProjection.ItemsPerPurchaseUnit,
                PurchaseUnitsPerOuterPack: unitProjection.PurchaseUnitsPerOuterPack,
                AllowDecimalQuantity: unitProjection.AllowDecimalQuantity,
                UnitConversions: unitProjection.UnitConversions,
                PricingTax: await ProjectPricingTaxAsync(tenantId, product.Id, cancellationToken),
                VariantConfiguration: command.VariantConfiguration,
                InitialBatchNumber: trackingValues.Batch,
                InitialExpiryDate: trackingValues.Expiry,
                InitialSerialNumber: trackingValues.Serial,
                InitialTrackingAssignedVariantId: trackingValues.AssignedVariantId));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return SaveProductDraftResult.Failure(new ApplicationError(
                "product.concurrency_conflict",
                "Product was modified by another user. Refresh and try again."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ProductSetupWizardDto?> GetSetupAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.ArchivedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var channelIds = await ResolvePosAndOnlineChannelIdsAsync(tenantId, cancellationToken);
        var images = await ProjectProductImagesAsync(tenantId, productId, cancellationToken);
        var categoryId = await GetPrimaryCategoryIdAsync(tenantId, productId, cancellationToken);
        var trackingFlags = await GetInventoryTrackingFlagsAsync(tenantId, productId, cancellationToken);

        var posSellable = false;
        var allowOnlineSale = false;
        if (channelIds.PosSalesChannelId.HasValue && channelIds.OnlineSalesChannelId.HasValue)
        {
            var flags = await GetChannelFlagsAsync(
                tenantId,
                productId,
                channelIds.PosSalesChannelId.Value,
                channelIds.OnlineSalesChannelId.Value,
                cancellationToken);
            posSellable = flags.PosSellable;
            allowOnlineSale = flags.AllowOnlineSale;
        }

        var categoryName = categoryId.HasValue
            ? await _dbContext.Categories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == categoryId.Value)
                .Select(x => x.CategoryName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var brandName = product.BrandId.HasValue
            ? await _dbContext.Brands
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == product.BrandId.Value)
                .Select(x => x.BrandName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var createdByName = product.CreatedByTenantUserId.HasValue
            ? await _dbContext.TenantUsers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == product.CreatedByTenantUserId.Value)
                .Select(x => x.DisplayName ?? x.FullName ?? x.Email)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var primaryImageUrl = images.FirstOrDefault()?.ImageUrl;

        var inventoryMethod = product.ProductStructure switch
        {
            ProductStructureConstants.Variant => "VARIANT_BASED",
            ProductStructureConstants.Bundle => "COMPONENT_BASED",
            _ => "PRODUCT_BASED"
        };

        var componentCount = await _dbContext.ComboDefinitions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == product.Id)
            .SelectMany(x => _dbContext.ComboComponents.Where(c => c.TenantId == tenantId && c.ComboDefinitionId == x.Id))
            .CountAsync(cancellationToken);

        var componentsConfigured = componentCount >= 2;
        var unitProjection = await ProjectProductUnitSettingsAsync(tenantId, product.Id, cancellationToken);
        var barcodeSkuProjection = await ProjectBarcodeSkuConfigurationAsync(tenantId, product.Id, cancellationToken);
        var trackingValues = await LoadInitialTrackingValuesAsync(tenantId, productId, cancellationToken);

        VariantConfigurationDto? variantConfiguration = null;
        var totalVariantCount = 0;
        var includedVariantCount = 0;
        if (string.Equals(product.ProductStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
        {
            variantConfiguration = await ProjectVariantConfigurationAsync(tenantId, product.Id, cancellationToken);
            if (variantConfiguration?.Variants is { Count: > 0 } variants)
            {
                totalVariantCount = variants.Count;
                includedVariantCount = variants.Count(v => v.Included);
            }
        }

        return new ProductSetupWizardDto(
            product.Id,
            product.ProductName,
            product.ProductCode,
            product.Status,
            product.DesiredPublishStatus,
            product.CurrentSetupStep,
            product.DraftSavedAt,
            product.RowVersion,
            categoryId,
            product.BrandId,
            product.ShortDescription,
            product.LongDescription,
            posSellable,
            trackingFlags.TrackInventory,
            trackingFlags.BatchTracking,
            trackingFlags.ExpiryTracking,
            trackingFlags.SerialTracking,
            product.ProductStructure,
            allowOnlineSale,
            images,
            CategoryName: categoryName,
            BrandName: brandName,
            CreatedByTenantUserId: product.CreatedByTenantUserId,
            CreatedByName: createdByName,
            CreatedAt: product.CreatedAt,
            Sku: null,
            PrimaryImageUrl: primaryImageUrl,
            InventoryMethod: inventoryMethod,
            ComponentCount: componentCount,
            ComponentsConfigured: componentsConfigured,
            TargetSetupStep: product.CurrentSetupStep,
            LastCompletedSetupStep: product.CurrentSetupStep,
            UnitModel: unitProjection.UnitModel,
            BaseUnitId: unitProjection.BaseUnitId,
            BaseUnitName: unitProjection.BaseUnitName,
            SellingUnitId: unitProjection.SellingUnitId,
            SellingUnitName: unitProjection.SellingUnitName,
            PurchaseUnitId: unitProjection.PurchaseUnitId,
            PurchaseUnitName: unitProjection.PurchaseUnitName,
            OuterPackUnitId: unitProjection.OuterPackUnitId,
            OuterPackUnitName: unitProjection.OuterPackUnitName,
            ItemsPerPurchaseUnit: unitProjection.ItemsPerPurchaseUnit,
            PurchaseUnitsPerOuterPack: unitProjection.PurchaseUnitsPerOuterPack,
            AllowDecimalQuantity: unitProjection.AllowDecimalQuantity,
            UnitConversions: unitProjection.UnitConversions,
            PricingTax: await ProjectPricingTaxAsync(tenantId, productId, cancellationToken),
            BarcodeSkuConfiguration: barcodeSkuProjection,
            VariantConfiguration: variantConfiguration,
            TotalVariantCount: totalVariantCount,
            IncludedVariantCount: includedVariantCount,
            InitialBatchNumber: trackingValues.Batch,
            InitialExpiryDate: trackingValues.Expiry,
            InitialSerialNumber: trackingValues.Serial,
            InitialTrackingAssignedVariantId: trackingValues.AssignedVariantId);
    }

    private async Task<(Guid? PosSalesChannelId, Guid? OnlineSalesChannelId, ApplicationError? Error)>
        ResolvePosAndOnlineChannelIdsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var rows = await (
            from channel in _dbContext.SalesChannels
            join platform in _dbContext.PlatformSalesChannels
                on channel.PlatformSalesChannelId equals platform.Id
            where channel.TenantId == tenantId &&
                  channel.Status == "ACTIVE" &&
                  (platform.ChannelCode == PlatformSalesChannelSeedConstants.PosChannelCode ||
                   platform.ChannelCode == "PHYSICAL" ||
                   platform.ChannelCode == "ONLINE")
            select new { platform.ChannelCode, channel.Id })
            .ToListAsync(cancellationToken);

        var posId = rows.FirstOrDefault(x =>
            x.ChannelCode == PlatformSalesChannelSeedConstants.PosChannelCode ||
            x.ChannelCode == "PHYSICAL")?.Id;
        var onlineId = rows.FirstOrDefault(x => x.ChannelCode == "ONLINE")?.Id;

        if (!posId.HasValue || !onlineId.HasValue)
        {
            var platformChannels = await _dbContext.PlatformSalesChannels.AsNoTracking().ToListAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            if (!posId.HasValue)
            {
                var platformPos = platformChannels.FirstOrDefault(x =>
                    x.ChannelCode == PlatformSalesChannelSeedConstants.PosChannelCode ||
                    x.ChannelCode == "PHYSICAL");

                if (platformPos == null)
                {
                    platformPos = PlatformSalesChannel.Create(
                        PlatformSalesChannelSeedConstants.PosChannelId,
                        PlatformSalesChannelSeedConstants.PosChannelCode,
                        PlatformSalesChannelSeedConstants.PosChannelName,
                        PlatformSalesChannelSeedConstants.PosChannelType,
                        now);
                    _dbContext.PlatformSalesChannels.Add(platformPos);
                }

                var newPosChannel = SalesChannel.Create(
                    Guid.NewGuid(),
                    tenantId,
                    platformPos.Id,
                    "POS Storefront",
                    "ACTIVE",
                    0,
                    now);
                _dbContext.SalesChannels.Add(newPosChannel);
                posId = newPosChannel.Id;
            }

            if (!onlineId.HasValue)
            {
                var platformOnline = platformChannels.FirstOrDefault(x => x.ChannelCode == "ONLINE");
                if (platformOnline == null)
                {
                    platformOnline = PlatformSalesChannel.Create(
                        PlatformSalesChannelSeedConstants.OnlineChannelId,
                        "ONLINE",
                        "Online Store",
                        "ONLINE",
                        now);
                    _dbContext.PlatformSalesChannels.Add(platformOnline);
                }

                var newOnlineChannel = SalesChannel.Create(
                    Guid.NewGuid(),
                    tenantId,
                    platformOnline.Id,
                    "E-Commerce Storefront",
                    "ACTIVE",
                    1,
                    now);
                _dbContext.SalesChannels.Add(newOnlineChannel);
                onlineId = newOnlineChannel.Id;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!posId.HasValue || !onlineId.HasValue)
        {
            return (null, null, new ApplicationError(
                "product.validation_failed",
                "Required sales channels (POS/ONLINE) are not provisioned for this tenant."));
        }

        return (posId, onlineId, null);
    }

    private async Task<(string Code, ApplicationError? Error)> EnsureUniqueProductCodeAsync(
        Guid tenantId,
        string? requestedCode,
        Guid? excludeProductId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedCode))
        {
            var code = ProductConstants.NormalizeCode(requestedCode);
            var exists = await ProductCodeExistsAsync(tenantId, code, excludeProductId, cancellationToken);
            if (exists)
            {
                return (string.Empty, new ApplicationError(
                    "product.code_exists",
                    "A product with the same Short Name / Internal Code already exists."));
            }
            return (code, null);
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var generated = $"DRF-{Guid.NewGuid():N}";
            if (generated.Length > ProductConstants.ProductCodeMaxLength)
            {
                generated = generated[..ProductConstants.ProductCodeMaxLength];
            }

            if (!await ProductCodeExistsAsync(tenantId, generated, excludeProductId, cancellationToken))
            {
                return (ProductConstants.NormalizeCode(generated), null);
            }
        }

        var fallback = $"DRF-{Guid.NewGuid():N}";
        return (ProductConstants.NormalizeCode(
            fallback.Length > ProductConstants.ProductCodeMaxLength
                ? fallback[..ProductConstants.ProductCodeMaxLength]
                : fallback), null);
    }

    private async Task ClearProductCategoriesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var links = await _dbContext.ProductCategories
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);

        if (links.Count > 0)
        {
            _dbContext.ProductCategories.RemoveRange(links);
        }
    }

    private async Task UpsertPrimaryCategoryAsync(
        Guid tenantId,
        Guid productId,
        Guid categoryId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var links = await _dbContext.ProductCategories
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);

        var primary = links.FirstOrDefault(x => x.IsPrimaryCategory) ?? links.FirstOrDefault();
        if (primary is null)
        {
            await _dbContext.ProductCategories.AddAsync(
                ProductCategory.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    categoryId,
                    isPrimaryCategory: true,
                    sortOrder: 0,
                    userId,
                    now),
                cancellationToken);
            return;
        }

        primary.ReassignCategory(categoryId, isPrimaryCategory: true, userId, now);

        foreach (var other in links.Where(x => x.Id != primary.Id && x.IsPrimaryCategory))
        {
            other.ReassignCategory(other.CategoryId, isPrimaryCategory: false, userId, now);
        }
    }

    private async Task UpsertChannelVisibilityAsync(
        Guid tenantId,
        Guid productId,
        Guid salesChannelId,
        bool enabled,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ProductChannelVisibilities
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.SalesChannelId == salesChannelId &&
                     x.ProductVariantId == null &&
                     x.Status != "DELETED",
                cancellationToken);

        if (existing is null)
        {
            await _dbContext.ProductChannelVisibilities.AddAsync(
                ProductChannelVisibility.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    productVariantId: null,
                    salesChannelId,
                    isVisible: enabled,
                    isOrderable: enabled,
                    availableFrom: null,
                    availableUntil: null,
                    status: "ACTIVE",
                    userId,
                    now),
                cancellationToken);
            return;
        }

        existing.UpdateFlags(enabled, enabled, userId, now);
    }

    private async Task<ApplicationError?> UpsertInventorySettingAsync(
        Guid tenantId,
        Guid productId,
        bool trackInventory,
        bool requiresBatchTracking,
        bool requiresExpiryTracking,
        bool requiresSerialTracking,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var setting = await _dbContext.ProductInventorySettings
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.ProductVariantId == null &&
                     x.Status != "DELETED",
                cancellationToken);

        if (setting is null)
        {
            var uomId = await GetDefaultInventoryUomIdAsync(tenantId, cancellationToken);
            if (!uomId.HasValue)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("trackInventory", "No inventory unit of measure is available for this tenant.")]);
            }

            await _dbContext.ProductInventorySettings.AddAsync(
                ProductInventorySetting.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    productVariantId: null,
                    uomId.Value,
                    isStockTracked: trackInventory,
                    allowNegativeStock: false,
                    requiresBatchTracking: requiresBatchTracking,
                    requiresExpiryTracking: requiresExpiryTracking,
                    requiresSerialTracking: requiresSerialTracking,
                    DefaultCostingMethod,
                    status: "ACTIVE",
                    userId,
                    now),
                cancellationToken);
            return null;
        }

        setting.UpdateProfile(
            setting.InventoryUomId,
            trackInventory,
            setting.AllowNegativeStock,
            requiresBatchTracking,
            requiresExpiryTracking,
            requiresSerialTracking,
            string.IsNullOrWhiteSpace(setting.CostingMethod) ? DefaultCostingMethod : setting.CostingMethod,
            userId,
            now);
        return null;
    }

    private async Task<ApplicationError?> LinkStagedMediaAsync(
        Guid tenantId,
        Guid productId,
        IReadOnlyList<Guid> stagedMediaAssetIds,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (stagedMediaAssetIds.Count == 0)
        {
            return null;
        }

        var distinctIds = stagedMediaAssetIds.Distinct().ToArray();
        var existingCount = await _dbContext.ProductImages
            .CountAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.Status == ProductConstants.ActiveStatus &&
                     x.ProductVariantId == null,
                cancellationToken);

        if (existingCount + distinctIds.Length > ProductConstants.MaxProductImages)
        {
            return new ApplicationError(
                "media.max_images_exceeded",
                $"A product can have at most {ProductConstants.MaxProductImages} images.");
        }

        var assets = await _dbContext.MediaAssets
            .Where(x =>
                x.TenantId == tenantId &&
                distinctIds.Contains(x.Id) &&
                (x.Status == StagedMediaStatus || x.Status == ProductConstants.ActiveStatus) &&
                x.AssetPurpose == ProductConstants.ProductImagePurpose)
            .ToListAsync(cancellationToken);

        if (assets.Count != distinctIds.Length)
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError(
                    "stagedMediaAssetIds",
                    "One or more staged media assets were not found or are not available.")]);
        }

        var alreadyLinked = await _dbContext.ProductImages
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.MediaAssetId != null &&
                distinctIds.Contains(x.MediaAssetId.Value) &&
                x.Status != ProductConstants.DeletedStatus)
            .Select(x => x.MediaAssetId!.Value)
            .ToListAsync(cancellationToken);

        var linkable = assets.Where(x => !alreadyLinked.Contains(x.Id)).ToList();
        if (linkable.Count == 0)
        {
            return null;
        }

        var hasPrimary = await _dbContext.ProductImages
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.Status == ProductConstants.ActiveStatus &&
                     x.ProductVariantId == null &&
                     x.IsPrimaryImage,
                cancellationToken);

        var maxSort = await _dbContext.ProductImages
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.ProductVariantId == null)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var nextSort = maxSort + 1;
        var assignPrimary = !hasPrimary;

        foreach (var asset in linkable)
        {
            var image = ProductImage.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                productVariantId: null,
                salesChannelId: null,
                asset.Id,
                altText: null,
                ProductConstants.ProductImagePurpose,
                nextSort++,
                isPrimaryImage: assignPrimary,
                ProductConstants.ActiveStatus,
                userId,
                now);

            await _dbContext.ProductImages.AddAsync(image, cancellationToken);
            asset.MarkActive(userId, now);
            assignPrimary = false;
        }

        return null;
    }

    private async Task<IReadOnlyList<TenantAdminProductImageResponse>> ProjectProductImagesAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var rawImages = await (
            from image in _dbContext.ProductImages.AsNoTracking()
            join media in _dbContext.MediaAssets.AsNoTracking()
                on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                equals new { media.TenantId, MediaAssetId = (Guid?)media.Id } into mediaJoin
            from media in mediaJoin.DefaultIfEmpty()
            where image.TenantId == tenantId &&
                  image.ProductId == productId &&
                  image.Status == ProductConstants.ActiveStatus &&
                  image.ProductVariantId == null
            orderby image.SortOrder, image.CreatedAt
            select new
            {
                image.Id,
                image.MediaAssetId,
                image.ProductVariantId,
                PublicUrl = media != null ? media.PublicUrl ?? string.Empty : string.Empty,
                image.AltText,
                image.ImagePurpose,
                image.SortOrder,
                image.IsPrimaryImage
            })
            .ToListAsync(cancellationToken);

        return rawImages.Select(image => new TenantAdminProductImageResponse(
            image.Id,
            image.MediaAssetId,
            image.ProductVariantId,
            _mediaReadUrlResolver?.ResolveReadUrl(image.PublicUrl) ?? image.PublicUrl,
            image.AltText,
            image.ImagePurpose,
            image.SortOrder,
            image.IsPrimaryImage)).ToList();
    }

    private async Task<Guid?> GetPrimaryCategoryIdAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .OrderByDescending(x => x.IsPrimaryCategory)
            .ThenBy(x => x.SortOrder)
            .Select(x => (Guid?)x.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<(bool TrackInventory, bool BatchTracking, bool ExpiryTracking, bool SerialTracking)> GetInventoryTrackingFlagsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var row = await _dbContext.ProductInventorySettings
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.ProductVariantId == null &&
                x.Status != "DELETED")
            .Select(x => new { x.IsStockTracked, x.RequiresBatchTracking, x.RequiresExpiryTracking, x.RequiresSerialTracking })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return (false, false, false, false);
        }

        return (row.IsStockTracked, row.RequiresBatchTracking, row.RequiresExpiryTracking, row.RequiresSerialTracking);
    }

    private async Task<(bool PosSellable, bool AllowOnlineSale)> GetChannelFlagsAsync(
        Guid tenantId,
        Guid productId,
        Guid posSalesChannelId,
        Guid onlineSalesChannelId,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.ProductChannelVisibilities
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.ProductVariantId == null &&
                x.Status != "DELETED" &&
                (x.SalesChannelId == posSalesChannelId || x.SalesChannelId == onlineSalesChannelId))
            .Select(x => new { x.SalesChannelId, x.IsVisible, x.IsOrderable })
            .ToListAsync(cancellationToken);

        var pos = rows.FirstOrDefault(x => x.SalesChannelId == posSalesChannelId);
        var online = rows.FirstOrDefault(x => x.SalesChannelId == onlineSalesChannelId);

        return (
            pos is not null && pos.IsVisible && pos.IsOrderable,
            online is not null && online.IsVisible && online.IsOrderable);
    }

    private async Task<ApplicationError?> ApplyUnitsPackConversionAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        SaveProductDraftCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var model = ProductUnitModelConstants.Normalize(command.UnitModel);
        var submittedUomIds = new List<Guid>();

        if (string.Equals(model, ProductUnitModelConstants.SingleUnit, StringComparison.OrdinalIgnoreCase))
        {
            if (command.BaseUnitId.HasValue && command.BaseUnitId.Value != Guid.Empty)
            {
                submittedUomIds.Add(command.BaseUnitId.Value);
            }
        }
        else
        {
            if (command.BaseUnitId.HasValue && command.BaseUnitId.Value != Guid.Empty) submittedUomIds.Add(command.BaseUnitId.Value);
            if (command.SellingUnitId.HasValue && command.SellingUnitId.Value != Guid.Empty) submittedUomIds.Add(command.SellingUnitId.Value);
            if (command.PurchaseUnitId.HasValue && command.PurchaseUnitId.Value != Guid.Empty) submittedUomIds.Add(command.PurchaseUnitId.Value);
            if (command.OuterPackUnitId.HasValue && command.OuterPackUnitId.Value != Guid.Empty) submittedUomIds.Add(command.OuterPackUnitId.Value);
        }

        if (submittedUomIds.Count > 0)
        {
            var distinctIds = submittedUomIds.Distinct().ToList();
            var validCount = await _dbContext.UnitOfMeasures
                .AsNoTracking()
                .Where(x => (x.TenantId == null || x.TenantId == tenantId) &&
                            x.Status == "ACTIVE" &&
                            distinctIds.Contains(x.Id))
                .Select(x => x.Id)
                .CountAsync(cancellationToken);

            if (validCount < distinctIds.Count)
            {
                return new ApplicationError(
                    "unit.uom_not_found",
                    "Selected unit of measure was not found or is inactive.");
            }
        }

        var setting = await _dbContext.ProductUnitSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId, cancellationToken);

        Guid? baseUomId;
        Guid? sellingUomId;
        Guid? purchaseUomId;
        Guid? outerPackUomId;
        decimal? itemsPerPurchase;
        decimal? unitsPerOuter;

        if (string.Equals(model, ProductUnitModelConstants.SingleUnit, StringComparison.OrdinalIgnoreCase))
        {
            baseUomId = command.BaseUnitId;
            sellingUomId = command.BaseUnitId;
            purchaseUomId = command.BaseUnitId;
            outerPackUomId = null;
            itemsPerPurchase = null;
            unitsPerOuter = null;
        }
        else
        {
            baseUomId = command.BaseUnitId;
            sellingUomId = command.SellingUnitId;
            purchaseUomId = command.PurchaseUnitId;
            outerPackUomId = command.OuterPackUnitId;
            itemsPerPurchase = command.ItemsPerPurchaseUnit;
            unitsPerOuter = command.OuterPackUnitId.HasValue ? command.PurchaseUnitsPerOuterPack : null;
        }

        if (setting == null)
        {
            setting = ProductUnitSetting.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                model,
                baseUomId,
                sellingUomId,
                purchaseUomId,
                outerPackUomId,
                itemsPerPurchase,
                unitsPerOuter,
                command.AllowDecimalQuantity,
                userId,
                now);

            await _dbContext.ProductUnitSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Update(
                model,
                baseUomId,
                sellingUomId,
                purchaseUomId,
                outerPackUomId,
                itemsPerPurchase,
                unitsPerOuter,
                command.AllowDecimalQuantity,
                userId,
                now);
        }

        var existingConversions = _dbContext.ProductUnitConversions
            .Where(x => x.TenantId == tenantId && x.ProductId == productId);
        _dbContext.ProductUnitConversions.RemoveRange(existingConversions);

        if (baseUomId.HasValue && baseUomId.Value != Guid.Empty)
        {
            var newConversions = new List<ProductUnitConversion>();

            if (string.Equals(model, ProductUnitModelConstants.SingleUnit, StringComparison.OrdinalIgnoreCase))
            {
                newConversions.Add(ProductUnitConversion.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    baseUomId.Value,
                    "BASE",
                    1.0m,
                    isBaseUnit: true,
                    isSellingUnit: true,
                    isPurchaseUnit: true,
                    isOuterPackUnit: false,
                    userId,
                    now));
            }
            else
            {
                var uomDict = new Dictionary<Guid, (string unitLevel, decimal factor, bool isBase, bool isSelling, bool isPurchase, bool isOuter)>();

                uomDict[baseUomId.Value] = ("BASE", 1.0m, true, false, false, false);

                if (purchaseUomId.HasValue && purchaseUomId.Value != Guid.Empty && itemsPerPurchase.HasValue)
                {
                    var factor = itemsPerPurchase.Value;
                    if (uomDict.TryGetValue(purchaseUomId.Value, out var existing))
                    {
                        uomDict[purchaseUomId.Value] = (existing.unitLevel, existing.factor, existing.isBase, existing.isSelling, true, existing.isOuter);
                    }
                    else
                    {
                        uomDict[purchaseUomId.Value] = ("PURCHASE", factor, false, false, true, false);
                    }
                }

                if (outerPackUomId.HasValue && outerPackUomId.Value != Guid.Empty && itemsPerPurchase.HasValue && unitsPerOuter.HasValue)
                {
                    var factor = itemsPerPurchase.Value * unitsPerOuter.Value;
                    if (uomDict.TryGetValue(outerPackUomId.Value, out var existing))
                    {
                        uomDict[outerPackUomId.Value] = (existing.unitLevel, existing.factor, existing.isBase, existing.isSelling, existing.isPurchase, true);
                    }
                    else
                    {
                        uomDict[outerPackUomId.Value] = ("OUTER_PACK", factor, false, false, false, true);
                    }
                }

                if (sellingUomId.HasValue && sellingUomId.Value != Guid.Empty && uomDict.TryGetValue(sellingUomId.Value, out var sellingExisting))
                {
                    uomDict[sellingUomId.Value] = (sellingExisting.unitLevel, sellingExisting.factor, sellingExisting.isBase, true, sellingExisting.isPurchase, sellingExisting.isOuter);
                }

                foreach (var kvp in uomDict)
                {
                    newConversions.Add(ProductUnitConversion.Create(
                        Guid.NewGuid(),
                        tenantId,
                        productId,
                        kvp.Key,
                        kvp.Value.unitLevel,
                        kvp.Value.factor,
                        kvp.Value.isBase,
                        kvp.Value.isSelling,
                        kvp.Value.isPurchase,
                        kvp.Value.isOuter,
                        userId,
                        now));
                }
            }

            await _dbContext.ProductUnitConversions.AddRangeAsync(newConversions, cancellationToken);

            var invSetting = await _dbContext.ProductInventorySettings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId && x.ProductVariantId == null, cancellationToken);

            if (invSetting != null)
            {
                invSetting.UpdateProfile(
                    baseUomId.Value,
                    invSetting.IsStockTracked,
                    invSetting.AllowNegativeStock,
                    invSetting.RequiresBatchTracking,
                    invSetting.RequiresExpiryTracking,
                    invSetting.RequiresSerialTracking,
                    invSetting.CostingMethod,
                    userId,
                    now);
            }
        }

        var auditLog = new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = userId,
            ActorType = "TENANT_USER",
            EntityType = "PRODUCT_UNITS_PACK_CONVERSION",
            EntityId = productId,
            Action = "PRODUCT_UNITS_PACK_CONVERSION_SAVED",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                unitModel = model,
                baseUomId,
                sellingUomId,
                purchaseUomId,
                outerPackUomId,
                itemsPerPurchaseUnit = itemsPerPurchase,
                purchaseUnitsPerOuterPack = unitsPerOuter,
                allowDecimalQuantity = command.AllowDecimalQuantity
            }),
            CreatedAt = now
        };
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);

        return null;
    }

    private async Task<UnitSettingsProjection> ProjectProductUnitSettingsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var setting = await _dbContext.ProductUnitSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == "ACTIVE", cancellationToken);

        if (setting == null)
        {
            return new UnitSettingsProjection();
        }

        var conversions = await (
            from c in _dbContext.ProductUnitConversions.AsNoTracking()
            join u in _dbContext.UnitOfMeasures.AsNoTracking() on c.UomId equals u.Id
            where c.TenantId == tenantId && c.ProductId == productId && c.Status == "ACTIVE"
            orderby c.ConversionToBaseFactor
            select new ProductUnitConversionResponse(
                c.UomId,
                u.UomCode,
                u.UomName,
                c.UnitLevel,
                c.ConversionToBaseFactor,
                c.IsBaseUnit,
                c.IsSellingUnit,
                c.IsPurchaseUnit,
                c.IsOuterPackUnit))
            .ToListAsync(cancellationToken);

        var uomIds = new List<Guid>();
        if (setting.BaseUomId.HasValue) uomIds.Add(setting.BaseUomId.Value);
        if (setting.SellingUomId.HasValue) uomIds.Add(setting.SellingUomId.Value);
        if (setting.PurchaseUomId.HasValue) uomIds.Add(setting.PurchaseUomId.Value);
        if (setting.OuterPackUomId.HasValue) uomIds.Add(setting.OuterPackUomId.Value);

        var uomNames = await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x => uomIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UomName, cancellationToken);

        string? baseUnitName = setting.BaseUomId.HasValue && uomNames.TryGetValue(setting.BaseUomId.Value, out var bn) ? bn : null;
        string? sellingUnitName = setting.SellingUomId.HasValue && uomNames.TryGetValue(setting.SellingUomId.Value, out var sn) ? sn : null;
        string? purchaseUnitName = setting.PurchaseUomId.HasValue && uomNames.TryGetValue(setting.PurchaseUomId.Value, out var pn) ? pn : null;
        string? outerPackUnitName = setting.OuterPackUomId.HasValue && uomNames.TryGetValue(setting.OuterPackUomId.Value, out var on) ? on : null;

        return new UnitSettingsProjection
        {
            UnitModel = setting.UnitModel,
            BaseUnitId = setting.BaseUomId,
            BaseUnitName = baseUnitName,
            SellingUnitId = setting.SellingUomId,
            SellingUnitName = sellingUnitName,
            PurchaseUnitId = setting.PurchaseUomId,
            PurchaseUnitName = purchaseUnitName,
            OuterPackUnitId = setting.OuterPackUomId,
            OuterPackUnitName = outerPackUnitName,
            ItemsPerPurchaseUnit = setting.ItemsPerPurchaseUnit,
            PurchaseUnitsPerOuterPack = setting.PurchaseUnitsPerOuterPack,
            AllowDecimalQuantity = setting.AllowDecimalQuantity,
            UnitConversions = conversions
        };
    }

    private sealed class UnitSettingsProjection
    {
        public string? UnitModel { get; set; }
        public Guid? BaseUnitId { get; set; }
        public string? BaseUnitName { get; set; }
        public Guid? SellingUnitId { get; set; }
        public string? SellingUnitName { get; set; }
        public Guid? PurchaseUnitId { get; set; }
        public string? PurchaseUnitName { get; set; }
        public Guid? OuterPackUnitId { get; set; }
        public string? OuterPackUnitName { get; set; }
        public decimal? ItemsPerPurchaseUnit { get; set; }
        public decimal? PurchaseUnitsPerOuterPack { get; set; }
        public bool AllowDecimalQuantity { get; set; }
        public IReadOnlyList<ProductUnitConversionResponse>? UnitConversions { get; set; }
    }

    private async Task<ApplicationError?> ApplyBarcodeSkuConfigurationAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        BarcodeSkuConfigurationDto configuration,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (configuration.Assignments == null || configuration.Assignments.Count == 0)
            return null;

        var variants = await _dbContext.ProductVariants
            .Where(v =>
                v.TenantId == tenantId &&
                v.ProductId == productId &&
                v.Status != ProductConstants.ArchivedStatus &&
                v.Status != ProductConstants.DeletedStatus)
            .ToListAsync(cancellationToken);

        var variantById = variants.ToDictionary(v => v.Id);
        var variantIds = variants.Select(v => v.Id).ToList();

        var existingBarcodes = await _dbContext.ProductBarcodes
            .Where(b => b.TenantId == tenantId && b.ProductId == productId && b.ProductVariantId != null && variantIds.Contains(b.ProductVariantId.Value))
            .ToListAsync(cancellationToken);

        // Prefer selling UOM from the variant for quantity_per_scan = 1 primary barcode.
        for (var i = 0; i < configuration.Assignments.Count; i++)
        {
            var assignment = configuration.Assignments[i];
            var prefix = $"barcodeSkuConfiguration.assignments[{i}]";

            ProductVariant? targetVariant = null;
            if (assignment.ProductVariantId.HasValue && assignment.ProductVariantId.Value != Guid.Empty)
            {
                if (!variantById.TryGetValue(assignment.ProductVariantId.Value, out targetVariant))
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        "Product validation failed.",
                        [
                            new ApplicationFieldError(
                                $"{prefix}.productVariantId",
                                "Variant does not belong to this product or is not an applicable Step 5 target.")
                        ]);
                }
            }
            else
            {
                targetVariant = variants.FirstOrDefault(v => v.IsDefaultVariant)
                    ?? variants.FirstOrDefault();
            }

            if (targetVariant == null)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [
                        new ApplicationFieldError(
                            $"{prefix}.productVariantId",
                            "No applicable variant found for identifier assignment.")
                    ]);
            }

            if (!string.IsNullOrWhiteSpace(assignment.Sku))
            {
                targetVariant.UpdateSku(assignment.Sku.Trim(), userId, now);
            }

            var existingBarcode = existingBarcodes
                .Where(b => b.ProductVariantId == targetVariant.Id && b.Status != ProductConstants.DeletedStatus)
                .OrderByDescending(b => b.IsPrimaryBarcode)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(assignment.Barcode))
            {
                var barcodeType = ProductBarcodeFormatValidator.NormalizeType(assignment.BarcodeType);
                if (barcodeType is null)
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        "Product validation failed.",
                        [
                            new ApplicationFieldError(
                                $"{prefix}.barcodeType",
                                "Barcode type is required when a barcode is provided.")
                        ]);
                }

                if (existingBarcode == null)
                {
                    var newBarcode = ProductBarcode.Create(
                        Guid.NewGuid(),
                        tenantId,
                        productId,
                        targetVariant.Id,
                        assignment.Barcode.Trim(),
                        barcodeType,
                        targetVariant.SalesUomId,
                        1m,
                        true,
                        ProductConstants.InactiveStatus,
                        userId,
                        now
                    );
                    await _dbContext.ProductBarcodes.AddAsync(newBarcode, cancellationToken);
                }
                else
                {
                    existingBarcode.UpdateIdentifier(assignment.Barcode.Trim(), barcodeType, userId, now);
                    if (existingBarcode.Status != ProductConstants.InactiveStatus && existingBarcode.Status != ProductConstants.ActiveStatus)
                    {
                        existingBarcode.Deactivate(userId, now);
                    }
                }
            }
            else if (existingBarcode != null &&
                     string.Equals(existingBarcode.Status, ProductConstants.InactiveStatus, StringComparison.OrdinalIgnoreCase))
            {
                // Clear draft (inactive) primary barcode safely without touching active historical rows.
                existingBarcode.Delete(userId, now);
            }
        }

        return null;
    }

    private async Task<ApplicationError?> ApplyPricingTaxConfigurationAsync(
        Guid tenantId,
        Guid userId,
        Product product,
        PricingTaxConfigurationDto configuration,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Partial update: omitted TaxExclusive must preserve existing Inclusive/Exclusive.
        if (configuration.TaxExclusive.HasValue)
        {
            product.UpdateTaxConfiguration(configuration.TaxExclusive.Value, userId, now);
        }

        // Optional product-level cost; omitted preserves existing.
        if (configuration.CostPrice.HasValue)
        {
            product.UpdateReferenceCost(configuration.CostPrice.Value, userId, now);
        }

        var defaultPriceList = await _dbContext.PriceLists
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE", cancellationToken);

        if (defaultPriceList == null)
        {
            return new ApplicationError("pricing.default_price_list_missing", "No default price list configured for tenant.");
        }

        var structure = ProductStructureConstants.Normalize(product.ProductStructure);
        var isVariant = string.Equals(structure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase);

        // Prefer Local during atomic wizard-create (variants may not be flushed yet).
        var allVariants = _dbContext.ProductVariants.Local
            .Where(v => v.TenantId == tenantId &&
                        v.ProductId == product.Id &&
                        v.Status != ProductConstants.DeletedStatus)
            .ToList();
        if (allVariants.Count == 0)
        {
            allVariants = await _dbContext.ProductVariants
                .Where(v => v.TenantId == tenantId &&
                            v.ProductId == product.Id &&
                            v.Status != ProductConstants.DeletedStatus)
                .ToListAsync(cancellationToken);
        }

        var includedVariants = allVariants
            .Where(v => v.IsSellable &&
                        v.Status != ProductConstants.ArchivedStatus)
            .OrderBy(v => v.Id)
            .ToList();

        var existingPriceItems = await _dbContext.PriceListItems
            .Where(x => x.TenantId == tenantId &&
                        x.PriceListId == defaultPriceList.Id &&
                        x.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        if (isVariant)
        {
            var priceError = await ApplyVariantPricingAsync(
                tenantId,
                userId,
                product,
                configuration,
                includedVariants,
                allVariants,
                existingPriceItems,
                defaultPriceList.Id,
                now,
                cancellationToken);
            if (priceError is not null)
            {
                return priceError;
            }
        }
        else
        {
            var priceError = ApplySimpleLikePricing(
                tenantId,
                userId,
                product,
                configuration,
                includedVariants,
                existingPriceItems,
                defaultPriceList.Id,
                now);
            if (priceError is not null)
            {
                return priceError;
            }
        }

        if (configuration.TaxClassId.HasValue)
        {
            var taxError = await ApplyProductTaxAssignmentsAsync(
                tenantId,
                userId,
                product,
                configuration.TaxClassId.Value,
                isVariant ? includedVariants.Select(v => (Guid?)v.Id).ToList() : BuildSimpleTaxVariantIds(includedVariants),
                allVariants,
                now,
                cancellationToken);
            if (taxError is not null)
            {
                return taxError;
            }
        }

        return null;
    }

    private static List<Guid?> BuildSimpleTaxVariantIds(IReadOnlyList<ProductVariant> includedVariants)
    {
        // SIMPLE/BUNDLE wizard creates a default variant; tax assignment follows that identity.
        if (includedVariants.Count > 0)
        {
            return includedVariants.Select(v => (Guid?)v.Id).ToList();
        }

        return [null];
    }

    private ApplicationError? ApplySimpleLikePricing(
        Guid tenantId,
        Guid userId,
        Product product,
        PricingTaxConfigurationDto configuration,
        IReadOnlyList<ProductVariant> includedVariants,
        List<PriceListItem> existingPriceItems,
        Guid defaultPriceListId,
        DateTimeOffset now)
    {
        // SIMPLE path: one applicable selling configuration. Ignore VariantPrices if accidentally sent.
        if (!configuration.StandardSellingPrice.HasValue && !configuration.DiscountPrice.HasValue)
        {
            return null; // tax/cost-only update
        }

        decimal finalSellingPrice = configuration.StandardSellingPrice ?? 0m;
        decimal? finalCompareAtPrice = null;

        if (configuration.DiscountPrice.HasValue &&
            configuration.StandardSellingPrice.HasValue &&
            configuration.DiscountPrice.Value < configuration.StandardSellingPrice.Value)
        {
            finalSellingPrice = configuration.DiscountPrice.Value;
            finalCompareAtPrice = configuration.StandardSellingPrice.Value;
        }

        if (finalSellingPrice < 0)
        {
            return new ApplicationError(
                "product.validation_failed",
                "Standard selling price cannot be negative.",
                [new ApplicationFieldError("pricingTax.standardSellingPrice", "Standard selling price cannot be negative.")]);
        }

        var targetVariantIds = includedVariants.Count > 0
            ? includedVariants.Select(v => (Guid?)v.Id).ToList()
            : new List<Guid?> { null };

        // Soft-delete any unexpected extra product price rows (keep only SIMPLE targets).
        foreach (var orphan in existingPriceItems.Where(x =>
                     x.Status == "ACTIVE" &&
                     !targetVariantIds.Contains(x.ProductVariantId)))
        {
            orphan.SoftDelete(userId, now);
        }

        foreach (var variantId in targetVariantIds)
        {
            UpsertActivePriceItem(
                _dbContext,
                existingPriceItems,
                tenantId,
                userId,
                product.Id,
                defaultPriceListId,
                variantId,
                finalSellingPrice,
                finalCompareAtPrice,
                now);
        }

        return null;
    }

    private async Task<ApplicationError?> ApplyVariantPricingAsync(
        Guid tenantId,
        Guid userId,
        Product product,
        PricingTaxConfigurationDto configuration,
        IReadOnlyList<ProductVariant> includedVariants,
        IReadOnlyList<ProductVariant> allVariants,
        List<PriceListItem> existingPriceItems,
        Guid defaultPriceListId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Soft-delete prices for excluded / archived variants — no leakage into Step 6 graph.
        var includedIdSet = includedVariants.Select(v => v.Id).ToHashSet();
        foreach (var item in existingPriceItems.Where(x =>
                     x.Status == "ACTIVE" &&
                     x.ProductVariantId.HasValue &&
                     !includedIdSet.Contains(x.ProductVariantId.Value)))
        {
            item.SoftDelete(userId, now);
        }

        // Soft-delete product-null "parent" price rows — Default Selling Price is not authoritative.
        foreach (var parentRow in existingPriceItems.Where(x =>
                     x.Status == "ACTIVE" &&
                     x.ProductVariantId is null))
        {
            parentRow.SoftDelete(userId, now);
        }

        Dictionary<Guid, decimal?>? priceByVariantId = null;

        if (configuration.VariantPrices is not null)
        {
            var lookupKeys = await BuildVariantLookupKeysAsync(
                tenantId,
                product.Id,
                includedVariants.ToList(),
                cancellationToken);

            var resolveError = ResolveVariantPriceCommands(
                configuration.VariantPrices,
                includedVariants,
                allVariants,
                lookupKeys,
                out priceByVariantId);
            if (resolveError is not null)
            {
                return resolveError;
            }
        }
        else if (configuration.StandardSellingPrice.HasValue || configuration.DiscountPrice.HasValue)
        {
            // NEVER fan one scalar across multiple included variants.
            if (includedVariants.Count > 1)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "VARIANT products require per-variant selling prices. Scalar StandardSellingPrice cannot be applied to multiple variants.",
                    [new ApplicationFieldError(
                        "pricingTax.variantPrices",
                        "Provide variantPrices for each included variant. Default Selling Price / Apply to All is a client helper only.")]);
            }

            if (includedVariants.Count == 1)
            {
                decimal selling = configuration.StandardSellingPrice ?? 0m;
                if (configuration.DiscountPrice.HasValue &&
                    configuration.StandardSellingPrice.HasValue &&
                    configuration.DiscountPrice.Value < configuration.StandardSellingPrice.Value)
                {
                    selling = configuration.DiscountPrice.Value;
                }

                priceByVariantId = new Dictionary<Guid, decimal?>
                {
                    [includedVariants[0].Id] = selling > 0 ? selling : null
                };
            }
        }

        if (priceByVariantId is null)
        {
            // Tax/cost-only update — preserve existing variant prices.
            return null;
        }

        // Full-snapshot semantics: every included variant appears in the map (missing → PENDING).
        foreach (var variant in includedVariants)
        {
            if (!priceByVariantId.ContainsKey(variant.Id))
            {
                priceByVariantId[variant.Id] = null;
            }
        }

        foreach (var variant in includedVariants)
        {
            var sellingPrice = priceByVariantId[variant.Id];
            var existingItem = existingPriceItems.FirstOrDefault(x =>
                x.ProductVariantId == variant.Id &&
                (x.Status == "ACTIVE" || x.Status == "DELETED" || x.Status == "INACTIVE"));

            if (!sellingPrice.HasValue || sellingPrice.Value <= 0)
            {
                // PENDING: clear any ACTIVE price so reopen does not show a stale amount.
                if (existingItem is not null && existingItem.Status == "ACTIVE")
                {
                    existingItem.SoftDelete(userId, now);
                }

                continue;
            }

            UpsertActivePriceItem(
                _dbContext,
                existingPriceItems,
                tenantId,
                userId,
                product.Id,
                defaultPriceListId,
                variant.Id,
                sellingPrice.Value,
                compareAtPrice: null, // VARIANT R1: no discount fan-out
                now);
        }

        return null;
    }

    private static ApplicationError? ResolveVariantPriceCommands(
        IReadOnlyList<VariantPriceConfigurationDto> commands,
        IReadOnlyList<ProductVariant> includedVariants,
        IReadOnlyList<ProductVariant> allVariants,
        IReadOnlyDictionary<Guid, HashSet<string>> variantLookupKeys,
        out Dictionary<Guid, decimal?> priceByVariantId)
    {
        priceByVariantId = new Dictionary<Guid, decimal?>();
        var includedById = includedVariants.ToDictionary(v => v.Id);
        var allById = allVariants.ToDictionary(v => v.Id);

        for (var i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            var hasId = cmd.ProductVariantId.HasValue && cmd.ProductVariantId.Value != Guid.Empty;
            var key = cmd.ClientCombinationKey?.Trim();
            var hasKey = !string.IsNullOrWhiteSpace(key);

            ProductVariant? byId = null;
            ProductVariant? byKey = null;

            if (hasId)
            {
                if (!allById.TryGetValue(cmd.ProductVariantId!.Value, out byId))
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        "Variant price references an unknown or foreign variant.",
                        [new ApplicationFieldError(
                            $"pricingTax.variantPrices[{i}].productVariantId",
                            "Product variant was not found for this product.")]);
                }

                if (!includedById.ContainsKey(byId.Id))
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        "Cannot price an excluded or archived variant.",
                        [new ApplicationFieldError(
                            $"pricingTax.variantPrices[{i}].productVariantId",
                            "Variant is not an included sellable variant.")]);
                }
            }

            if (hasKey)
            {
                byKey = includedVariants.FirstOrDefault(v =>
                    VariantLookupMatches(v, key!, variantLookupKeys));

                if (byKey is null)
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        "Variant price clientCombinationKey did not resolve to an included variant.",
                        [new ApplicationFieldError(
                            $"pricingTax.variantPrices[{i}].clientCombinationKey",
                            "clientCombinationKey did not match an included variant.")]);
                }
            }

            if (byId is not null && byKey is not null && byId.Id != byKey.Id)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "productVariantId and clientCombinationKey resolve to different variants.",
                    [new ApplicationFieldError(
                        $"pricingTax.variantPrices[{i}]",
                        "Conflicting productVariantId and clientCombinationKey.")]);
            }

            var resolved = byId ?? byKey;
            if (resolved is null)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Each variant price must include productVariantId or clientCombinationKey.",
                    [new ApplicationFieldError(
                        $"pricingTax.variantPrices[{i}]",
                        "Variant identity is required.")]);
            }

            if (priceByVariantId.ContainsKey(resolved.Id))
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Duplicate variant price command.",
                    [new ApplicationFieldError(
                        $"pricingTax.variantPrices[{i}]",
                        "Duplicate variant identity in variantPrices.")]);
            }

            priceByVariantId[resolved.Id] = cmd.SellingPrice;
        }

        return null;
    }

    private static void UpsertActivePriceItem(
        EPosDbContext dbContext,
        List<PriceListItem> existingPriceItems,
        Guid tenantId,
        Guid userId,
        Guid productId,
        Guid defaultPriceListId,
        Guid? variantId,
        decimal sellingPrice,
        decimal? compareAtPrice,
        DateTimeOffset now)
    {
        var existingItem = existingPriceItems.FirstOrDefault(x => x.ProductVariantId == variantId);
        if (existingItem != null)
        {
            existingItem.UpdateProfile(
                sellingPrice,
                compareAtPrice,
                existingItem.MinQuantity > 0 ? existingItem.MinQuantity : 1m,
                existingItem.ValidFrom,
                existingItem.ValidUntil,
                "ACTIVE",
                userId,
                now);
            return;
        }

        var newItem = PriceListItem.Create(
            Guid.NewGuid(),
            tenantId,
            defaultPriceListId,
            productId,
            variantId,
            null,
            sellingPrice,
            compareAtPrice,
            1m,
            null,
            null,
            "ACTIVE",
            userId,
            now);
        existingPriceItems.Add(newItem);
        dbContext.PriceListItems.Add(newItem);
    }

    private async Task<ApplicationError?> ApplyProductTaxAssignmentsAsync(
        Guid tenantId,
        Guid userId,
        Product product,
        Guid targetTaxClassId,
        IReadOnlyList<Guid?> targetVariantIds,
        IReadOnlyList<ProductVariant> allVariants,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingTaxAssignments = await _dbContext.ProductTaxAssignments
            .Where(x => x.TenantId == tenantId && x.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        var activeAssignments = existingTaxAssignments
            .Where(x => string.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Option B: retaining the same TaxClassId already ACTIVE on the product does not require
        // the Tax Setup to still be ACTIVE. Only NEW or CHANGED assignments must be ACTIVE.
        var isUnchangedExistingAssignment =
            activeAssignments.Count > 0 &&
            activeAssignments.All(x => x.TaxClassId == targetTaxClassId);

        if (!isUnchangedExistingAssignment)
        {
            var taxClass = await _dbContext.TaxClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.Id == targetTaxClassId,
                    cancellationToken);

            if (taxClass is null)
            {
                return new ApplicationError(
                    "tax.not_found",
                    "Tax Setup was not found for this tenant.");
            }

            if (!string.Equals(taxClass.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return new ApplicationError(
                    "tax.inactive_cannot_assign",
                    "An inactive Tax Setup cannot be newly assigned to a product.");
            }
        }

        var targetIdSet = targetVariantIds.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var targetsIncludeNull = targetVariantIds.Any(id => !id.HasValue);

        foreach (var assignment in activeAssignments)
        {
            var isTarget = assignment.ProductVariantId.HasValue
                ? targetIdSet.Contains(assignment.ProductVariantId.Value)
                : targetsIncludeNull;

            if (!isTarget)
            {
                assignment.SoftDelete(userId, now);
            }
        }

        foreach (var variantId in targetVariantIds)
        {
            var existingAssignment = existingTaxAssignments.FirstOrDefault(x => x.ProductVariantId == variantId);
            if (existingAssignment != null)
            {
                existingAssignment.UpdateAssignment(
                    targetTaxClassId,
                    existingAssignment.AppliesFrom,
                    existingAssignment.AppliesUntil,
                    "ACTIVE",
                    userId,
                    now);
            }
            else
            {
                var newAssignment = ProductTaxAssignment.Create(
                    tenantId,
                    product.Id,
                    variantId,
                    targetTaxClassId,
                    null,
                    null,
                    userId,
                    now);
                await _dbContext.ProductTaxAssignments.AddAsync(newAssignment, cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// Projects the saved Barcode &amp; SKU data from DB into the response DTO
    /// so Step 5 loads correctly when a draft is resumed.
    /// Reads: product_variants (SKU) + product_barcodes (primary barcode per variant).
    /// Targets: sellable, non-archived/deleted variants only.
    /// </summary>
    private async Task<BarcodeSkuConfigurationDto?> ProjectBarcodeSkuConfigurationAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(v =>
                v.TenantId == tenantId &&
                v.ProductId == productId &&
                v.IsSellable &&
                v.Status != ProductConstants.ArchivedStatus &&
                v.Status != ProductConstants.DeletedStatus)
            .OrderBy(v => v.Id)
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
            return null;

        var variantIds = variants.Select(v => v.Id).ToList();

        var barcodes = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId &&
                        b.ProductId == productId &&
                        b.ProductVariantId != null &&
                        variantIds.Contains(b.ProductVariantId!.Value) &&
                        b.Status != ProductConstants.DeletedStatus)
            .ToListAsync(cancellationToken);

        var assignments = new List<BarcodeSkuAssignmentDto>();
        var identifierTargets = new List<Step5IdentifierTargetDto>();

        foreach (var variant in variants)
        {
            var hasSku = !string.IsNullOrWhiteSpace(variant.Sku);

            var primaryBarcode = barcodes
                .Where(b => b.ProductVariantId == variant.Id)
                .OrderByDescending(b => b.IsPrimaryBarcode)
                .FirstOrDefault();

            var hasBarcode = primaryBarcode != null && !string.IsNullOrWhiteSpace(primaryBarcode.Barcode);
            var displayName = variant.VariantName.Trim().Length > 0
                ? variant.VariantName
                : variant.VariantCode;
            var clientKey = !string.IsNullOrWhiteSpace(variant.OptionCombinationHash)
                ? variant.OptionCombinationHash
                : variant.Id.ToString();

            var complete = hasSku &&
                (primaryBarcode == null ||
                 string.IsNullOrWhiteSpace(primaryBarcode.Barcode) ||
                 ProductBarcodeFormatValidator.Validate(primaryBarcode.Barcode, primaryBarcode.BarcodeType) is null);

            var status = hasSku
                ? (complete ? "COMPLETE" : "INVALID")
                : "INCOMPLETE";

            identifierTargets.Add(new Step5IdentifierTargetDto(
                ProductVariantId: variant.Id,
                DisplayName: displayName,
                IsAssigned: hasSku || hasBarcode));

            // Always project every target row so Flutter can hydrate the full table.
            assignments.Add(new BarcodeSkuAssignmentDto(
                ProductVariantId: variant.Id,
                DisplayName: displayName,
                Sku: hasSku ? variant.Sku : string.Empty,
                Barcode: hasBarcode ? primaryBarcode!.Barcode : null,
                Status: status,
                ClientCombinationKey: clientKey,
                BarcodeType: primaryBarcode?.BarcodeType));
        }

        return new BarcodeSkuConfigurationDto(
            IdentifierTargets: identifierTargets,
            Assignments: assignments);
    }

    /// <summary>
    /// Projects saved variant configuration from DB for GET /setup draft reopen (VARIANT products only).
    /// </summary>
    private async Task<VariantConfigurationDto?> ProjectVariantConfigurationAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var options = await _dbContext.ProductOptions
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId &&
                        o.ProductId == productId &&
                        o.Status != ProductConstants.ArchivedStatus &&
                        o.Status != ProductConstants.DeletedStatus)
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.OptionCode)
            .ToListAsync(cancellationToken);

        if (options.Count == 0)
        {
            return null;
        }

        var optionIds = options.Select(o => o.Id).ToList();
        var values = await _dbContext.ProductOptionValues
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId &&
                        optionIds.Contains(v.ProductOptionId) &&
                        v.Status != ProductConstants.ArchivedStatus &&
                        v.Status != ProductConstants.DeletedStatus)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.ValueCode)
            .ToListAsync(cancellationToken);

        var valuesByOptionId = values
            .GroupBy(v => v.ProductOptionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var optionDtos = options.Select(option =>
        {
            var optionValues = valuesByOptionId.TryGetValue(option.Id, out var list)
                ? list
                : [];

            return new VariantConfigurationOptionDto(
                option.Id,
                option.SourceOptionTemplateId,
                option.OptionCode,
                option.OptionName,
                option.OptionType,
                option.InputType,
                option.SortOrder,
                optionValues.Select(val => new VariantConfigurationOptionValueDto(
                    val.Id,
                    val.SourceOptionTemplateValueId,
                    val.ValueCode,
                    val.ValueName,
                    val.DisplayName,
                    val.ColorHex,
                    val.SortOrder,
                    val.ImageMediaAssetId)).ToList());
        }).ToList();

        var activeVariants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId &&
                        v.ProductId == productId &&
                        v.Status != ProductConstants.ArchivedStatus)
            .OrderBy(v => v.VariantCode)
            .ToListAsync(cancellationToken);

        var archivedVariants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId &&
                        v.ProductId == productId &&
                        v.Status == ProductConstants.ArchivedStatus)
            .ToListAsync(cancellationToken);

        var allVariantIds = activeVariants.Select(v => v.Id)
            .Concat(archivedVariants.Select(v => v.Id))
            .ToList();

        var variantMappings = allVariantIds.Count == 0
            ? []
            : await _dbContext.ProductVariantOptionValues
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId &&
                            m.ProductId == productId &&
                            allVariantIds.Contains(m.ProductVariantId))
                .ToListAsync(cancellationToken);

        var valueById = values.ToDictionary(v => v.Id);
        var optionById = options.ToDictionary(o => o.Id);

        string BuildClientKey(Guid variantId, IReadOnlyList<VariantConfigurationSelectedValueDto> selectedValues)
        {
            var templatePairs = selectedValues
                .Where(sv => sv.SourceOptionTemplateId.HasValue && sv.SourceOptionTemplateValueId.HasValue)
                .Select(sv => (sv.SourceOptionTemplateId!.Value, sv.SourceOptionTemplateValueId!.Value))
                .ToList();

            if (templatePairs.Count == selectedValues.Count && templatePairs.Count > 0)
            {
                return ProductVariantClientKeyHelper.GenerateClientCombinationKey(templatePairs);
            }

            return string.Join(";", selectedValues.Select(v =>
                $"{v.SourceOptionTemplateId?.ToString("D") ?? v.OptionName}:{v.SourceOptionTemplateValueId?.ToString("D") ?? v.ValueName}"));
        }

        List<VariantConfigurationSelectedValueDto> BuildSelectedValues(Guid variantId)
        {
            return variantMappings
                .Where(m => m.ProductVariantId == variantId)
                .Select(m =>
                {
                    valueById.TryGetValue(m.ProductOptionValueId, out var val);
                    optionById.TryGetValue(m.ProductOptionId, out var opt);
                    return new VariantConfigurationSelectedValueDto(
                        opt?.SourceOptionTemplateId,
                        val?.SourceOptionTemplateValueId,
                        opt?.OptionName,
                        val?.ValueName);
                })
                .OrderBy(sv => sv.SourceOptionTemplateId?.ToString("D") ?? sv.OptionName ?? string.Empty,
                    StringComparer.Ordinal)
                .ToList();
        }

        var variantDtos = activeVariants.Select(variant =>
        {
            var selectedValues = BuildSelectedValues(variant.Id);
            var clientKey = BuildClientKey(variant.Id, selectedValues);

            return new VariantConfigurationVariantDto(
                clientKey,
                variant.Id,
                variant.VariantCode,
                variant.OptionCombinationHash,
                variant.VariantName,
                variant.VariantName,
                variant.IsSellable,
                variant.Status,
                null,
                selectedValues);
        }).ToList();

        var excluded = archivedVariants
            .Select(v =>
            {
                var selectedValues = BuildSelectedValues(v.Id);
                return new VariantConfigurationDeletedCombinationDto(
                    BuildClientKey(v.Id, selectedValues),
                    v.Id,
                    v.OptionCombinationHash);
            })
            .ToList();

        return new VariantConfigurationDto(optionDtos, variantDtos, excluded);
    }

    public async Task<IReadOnlyList<ApplicationFieldError>> ValidateVariantConfigurationCatalogAsync(
        Guid tenantId,
        Guid? productId,
        VariantConfigurationDto configuration,
        CancellationToken cancellationToken)
    {
        var fieldErrors = new List<ApplicationFieldError>();
        if (configuration.Options is null || configuration.Options.Count == 0)
        {
            return fieldErrors;
        }

        var templateIds = configuration.Options
            .Where(o => o.SourceOptionTemplateId.HasValue)
            .Select(o => o.SourceOptionTemplateId!.Value)
            .Distinct()
            .ToList();

        var valueIds = configuration.Options
            .SelectMany(o => o.Values ?? [])
            .Where(v => v.SourceOptionTemplateValueId.HasValue)
            .Select(v => v.SourceOptionTemplateValueId!.Value)
            .Distinct()
            .ToList();

        var productOptionIds = configuration.Options
            .Where(o => o.ProductOptionId.HasValue)
            .Select(o => o.ProductOptionId!.Value)
            .Distinct()
            .ToList();

        var productOptionValueIds = configuration.Options
            .SelectMany(o => o.Values ?? [])
            .Where(v => v.ProductOptionValueId.HasValue)
            .Select(v => v.ProductOptionValueId!.Value)
            .Distinct()
            .ToList();

        var templates = templateIds.Count == 0
            ? new Dictionary<Guid, ProductOptionTemplate>()
            : await _dbContext.ProductOptionTemplates
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

        var templateValues = valueIds.Count == 0
            ? new Dictionary<Guid, ProductOptionTemplateValue>()
            : await _dbContext.ProductOptionTemplateValues
                .AsNoTracking()
                .Where(v => valueIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, cancellationToken);

        var persistedOptions = productOptionIds.Count == 0
            ? new Dictionary<Guid, ProductOption>()
            : await _dbContext.ProductOptions
                .AsNoTracking()
                .Where(o => o.TenantId == tenantId && productOptionIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, cancellationToken);

        var persistedValues = productOptionValueIds.Count == 0
            ? new Dictionary<Guid, ProductOptionValue>()
            : await _dbContext.ProductOptionValues
                .AsNoTracking()
                .Where(v => v.TenantId == tenantId && productOptionValueIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, cancellationToken);

        var seenTemplateIds = new HashSet<Guid>();

        for (var i = 0; i < configuration.Options.Count; i++)
        {
            var option = configuration.Options[i];

            if (option.ProductOptionId.HasValue)
            {
                if (!persistedOptions.TryGetValue(option.ProductOptionId.Value, out var persistedOption))
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].productOptionId",
                        "Product option was not found for this tenant.",
                        "product.option_not_found"));
                }
                else if (productId.HasValue && persistedOption.ProductId != productId.Value)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].productOptionId",
                        "Product option does not belong to this product.",
                        "product.option_cross_product_reference"));
                }
            }

            if (option.SourceOptionTemplateId.HasValue)
            {
                if (!seenTemplateIds.Add(option.SourceOptionTemplateId.Value))
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].sourceOptionTemplateId",
                        "Duplicate variant attribute selection is not allowed.",
                        "product.duplicate_attribute"));
                }

                if (!templates.TryGetValue(option.SourceOptionTemplateId.Value, out var template))
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].sourceOptionTemplateId",
                        "Variant attribute template was not found.",
                        "product.option_template_not_found"));
                }
                else if (!string.Equals(template.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].sourceOptionTemplateId",
                        "Variant attribute template is not active.",
                        "product.option_template_inactive"));
                }
            }
            else if (!option.ProductOptionId.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"variantConfiguration.options[{i}].sourceOptionTemplateId",
                    "Variant attribute must reference a valid template or persisted product option.",
                    "product.option_template_required"));
            }

            if (option.Values is null || option.Values.Count == 0)
            {
                continue;
            }

            var seenValueIds = new HashSet<Guid>();
            for (var j = 0; j < option.Values.Count; j++)
            {
                var value = option.Values[j];

                if (value.ProductOptionValueId.HasValue)
                {
                    if (!persistedValues.TryGetValue(value.ProductOptionValueId.Value, out var persistedValue))
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variantConfiguration.options[{i}].values[{j}].productOptionValueId",
                            "Product option value was not found for this tenant.",
                            "product.option_value_not_found"));
                    }
                    else if (productId.HasValue &&
                             persistedOptions.TryGetValue(persistedValue.ProductOptionId, out var parentOption) &&
                             parentOption.ProductId != productId.Value)
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variantConfiguration.options[{i}].values[{j}].productOptionValueId",
                            "Product option value does not belong to this product.",
                            "product.option_value_cross_product_reference"));
                    }
                    else if (option.ProductOptionId.HasValue &&
                             persistedValue.ProductOptionId != option.ProductOptionId.Value)
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variantConfiguration.options[{i}].values[{j}].productOptionValueId",
                            "Product option value does not belong to the selected attribute.",
                            "product.option_value_not_owned_by_attribute"));
                    }
                }

                if (value.SourceOptionTemplateValueId.HasValue)
                {
                    if (!seenValueIds.Add(value.SourceOptionTemplateValueId.Value))
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variantConfiguration.options[{i}].values[{j}].sourceOptionTemplateValueId",
                            "Duplicate value selection is not allowed within the same attribute.",
                            "product.duplicate_option_value"));
                    }

                    if (!templateValues.TryGetValue(value.SourceOptionTemplateValueId.Value, out var templateValue))
                    {
                        fieldErrors.Add(new ApplicationFieldError(
                            $"variantConfiguration.options[{i}].values[{j}].sourceOptionTemplateValueId",
                            "Variant attribute value was not found.",
                            "product.option_template_value_not_found"));
                    }
                    else
                    {
                        if (!string.Equals(templateValue.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                        {
                            fieldErrors.Add(new ApplicationFieldError(
                                $"variantConfiguration.options[{i}].values[{j}].sourceOptionTemplateValueId",
                                "Variant attribute value is not active.",
                                "product.option_template_value_inactive"));
                        }

                        if (option.SourceOptionTemplateId.HasValue &&
                            templateValue.OptionTemplateId != option.SourceOptionTemplateId.Value)
                        {
                            fieldErrors.Add(new ApplicationFieldError(
                                $"variantConfiguration.options[{i}].values[{j}].sourceOptionTemplateValueId",
                                "Selected value does not belong to the selected attribute.",
                                "product.option_value_not_owned_by_attribute"));
                        }
                    }
                }
                else if (!value.ProductOptionValueId.HasValue)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].values[{j}].sourceOptionTemplateValueId",
                        "Variant attribute value must reference a valid template or persisted product option value.",
                        "product.option_template_value_required"));
                }
            }
        }

        return fieldErrors;
    }

    private async Task<PricingTaxResponseDto?> ProjectPricingTaxAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == productId, cancellationToken);

        if (product == null)
            return null;

        var structure = ProductStructureConstants.Normalize(product.ProductStructure);
        var isVariant = string.Equals(structure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase);

        var defaultPriceList = await _dbContext.PriceLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE", cancellationToken);

        decimal? costPrice = product.ReferenceCostPrice;
        bool taxExclusive = product.IsTaxExclusive;

        Guid? taxClassId = null;
        string? taxName = null;
        decimal? taxRatePercentage = null;

        var taxAssignment = await _dbContext.ProductTaxAssignments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.ProductId == productId &&
                        x.Status == "ACTIVE")
            .OrderBy(x => x.ProductVariantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (taxAssignment != null)
        {
            taxClassId = taxAssignment.TaxClassId;
            var taxClass = await _dbContext.TaxClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == taxAssignment.TaxClassId, cancellationToken);

            if (taxClass != null)
            {
                taxName = taxClass.TaxClassName;
                taxRatePercentage = await ResolveEffectiveTaxRatePercentAsync(
                    tenantId,
                    taxClass.Id,
                    taxClass.TaxTreatment,
                    cancellationToken);
            }
        }

        if (isVariant)
        {
            var includedVariants = await _dbContext.ProductVariants
                .AsNoTracking()
                .Where(v => v.TenantId == tenantId &&
                            v.ProductId == productId &&
                            v.IsSellable &&
                            v.Status != ProductConstants.ArchivedStatus &&
                            v.Status != ProductConstants.DeletedStatus)
                .OrderBy(v => v.Id)
                .ToListAsync(cancellationToken);

            var variantIds = includedVariants.Select(v => v.Id).ToList();
            var priceItems = defaultPriceList == null || variantIds.Count == 0
                ? []
                : await _dbContext.PriceListItems
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId &&
                                x.PriceListId == defaultPriceList.Id &&
                                x.ProductId == productId &&
                                x.Status == "ACTIVE" &&
                                x.ProductVariantId.HasValue &&
                                variantIds.Contains(x.ProductVariantId.Value))
                    .ToListAsync(cancellationToken);

            var priceByVariant = priceItems
                .GroupBy(x => x.ProductVariantId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First());

            var variantPrices = new List<VariantPriceResponseDto>(includedVariants.Count);
            foreach (var variant in includedVariants)
            {
                decimal? selling = null;
                if (priceByVariant.TryGetValue(variant.Id, out var item) && item.SellingPrice > 0)
                {
                    selling = item.SellingPrice;
                }

                var displayName = !string.IsNullOrWhiteSpace(variant.VariantName)
                    ? variant.VariantName
                    : variant.VariantCode;

                variantPrices.Add(new VariantPriceResponseDto(
                    ProductVariantId: variant.Id,
                    ClientCombinationKey: variant.OptionCombinationHash ?? variant.Id.ToString(),
                    SellingPrice: selling,
                    DisplayName: displayName,
                    Sku: variant.Sku));
            }

            var priced = variantPrices.Where(v => v.SellingPrice.HasValue && v.SellingPrice.Value > 0).ToList();
            decimal? priceFrom = priced.Count == 0 ? null : priced.Min(v => v.SellingPrice);
            decimal? priceTo = priced.Count == 0 ? null : priced.Max(v => v.SellingPrice);

            return new PricingTaxResponseDto(
                CostPrice: costPrice,
                StandardSellingPrice: null,
                DiscountPrice: null,
                EffectiveSellingPrice: null,
                DiscountAmount: null,
                DiscountPercentage: null,
                TaxClassId: taxClassId,
                TaxName: taxName,
                TaxRatePercentage: taxRatePercentage,
                TaxExclusive: taxExclusive,
                VariantPrices: variantPrices,
                PricedVariantCount: priced.Count,
                PendingVariantCount: variantPrices.Count - priced.Count,
                PriceFrom: priceFrom,
                PriceTo: priceTo);
        }

        decimal? standardSellingPrice = null;
        decimal? discountPrice = null;
        decimal? effectiveSellingPrice = null;
        decimal? discountAmount = null;
        decimal? discountPercentage = null;

        if (defaultPriceList != null)
        {
            var priceItem = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.PriceListId == defaultPriceList.Id &&
                            x.ProductId == productId &&
                            x.Status == "ACTIVE")
                .OrderByDescending(x => x.ProductVariantId.HasValue)
                .ThenByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (priceItem != null)
            {
                if (priceItem.CompareAtPrice.HasValue && priceItem.CompareAtPrice.Value > priceItem.SellingPrice)
                {
                    standardSellingPrice = priceItem.CompareAtPrice.Value;
                    discountPrice = priceItem.SellingPrice;
                    effectiveSellingPrice = priceItem.SellingPrice;
                    discountAmount = standardSellingPrice - discountPrice;
                    if (standardSellingPrice > 0)
                        discountPercentage = Math.Round((discountAmount.Value / standardSellingPrice.Value) * 100, 2);
                }
                else
                {
                    standardSellingPrice = priceItem.SellingPrice;
                    effectiveSellingPrice = priceItem.SellingPrice;
                }
            }
        }

        return new PricingTaxResponseDto(
            costPrice,
            standardSellingPrice,
            discountPrice,
            effectiveSellingPrice,
            discountAmount,
            discountPercentage,
            taxClassId,
            taxName,
            taxRatePercentage,
            taxExclusive);
    }

    private async Task<decimal?> ResolveEffectiveTaxRatePercentAsync(
        Guid tenantId,
        Guid taxClassId,
        string? taxTreatment,
        CancellationToken cancellationToken)
    {
        if (string.Equals(taxTreatment, "EXEMPT", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var timezone = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.DefaultTimezone)
            .FirstOrDefaultAsync(cancellationToken);

        var businessToday = TaxRateResolution.ToBusinessDate(DateTimeOffset.UtcNow, timezone);

        var rates = await (
            from classRate in _dbContext.TaxClassRates.AsNoTracking()
            join rate in _dbContext.TaxRates.AsNoTracking()
                on new { classRate.TenantId, Id = classRate.TaxRateId }
                equals new { rate.TenantId, rate.Id }
            where classRate.TenantId == tenantId &&
                  classRate.TaxClassId == taxClassId &&
                  classRate.Status != "DELETED" &&
                  rate.Status != "DELETED"
            select rate
        ).ToListAsync(cancellationToken);

        var current = TaxRateResolution.ResolveCurrent(rates, businessToday);
        if (current is not null)
        {
            return current.RatePercent;
        }

        if (string.Equals(taxTreatment, "ZERO_RATED", StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        return null;
    }
    
    private async Task SoftDeleteVariantDownstreamPricingAsync(
        Guid tenantId,
        Guid productId,
        Guid variantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await SoftDeletePricingGraphForProductVariantsAsync(
            tenantId,
            productId,
            [variantId],
            updatedByUserId: null,
            now,
            cancellationToken);
    }

    private async Task SoftDeletePricingGraphForProductVariantsAsync(
        Guid tenantId,
        Guid productId,
        IReadOnlyList<Guid> variantIds,
        Guid? updatedByUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return;
        }

        var priceItems = await _dbContext.PriceListItems
            .Where(x => x.TenantId == tenantId &&
                        x.ProductId == productId &&
                        x.ProductVariantId.HasValue &&
                        variantIds.Contains(x.ProductVariantId.Value) &&
                        x.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        foreach (var item in priceItems)
        {
            item.SoftDelete(updatedByUserId, now);
        }

        var taxAssignments = await _dbContext.ProductTaxAssignments
            .Where(x => x.TenantId == tenantId &&
                        x.ProductId == productId &&
                        x.ProductVariantId.HasValue &&
                        variantIds.Contains(x.ProductVariantId.Value) &&
                        x.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        foreach (var assignment in taxAssignments)
        {
            assignment.SoftDelete(updatedByUserId, now);
        }
    }

    private async Task<ApplicationError?> ValidatePublishPricingAsync(
        Guid tenantId,
        Product product,
        CancellationToken cancellationToken)
    {
        var hasTax = await _dbContext.ProductTaxAssignments
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId &&
                           x.ProductId == product.Id &&
                           x.Status == "ACTIVE",
                cancellationToken);

        if (!hasTax)
        {
            return new ApplicationError(
                "product.validation_failed",
                "Tax assignment is required before publish.",
                [new ApplicationFieldError("pricingTax.taxClassId", "Tax class is required.")]);
        }

        var defaultPriceList = await _dbContext.PriceLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE", cancellationToken);

        if (defaultPriceList is null)
        {
            return new ApplicationError("pricing.default_price_list_missing", "No default price list configured for tenant.");
        }

        var structure = ProductStructureConstants.Normalize(product.ProductStructure);
        if (string.Equals(structure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
        {
            var includedIds = await _dbContext.ProductVariants
                .AsNoTracking()
                .Where(v => v.TenantId == tenantId &&
                            v.ProductId == product.Id &&
                            v.IsSellable &&
                            v.Status != ProductConstants.ArchivedStatus &&
                            v.Status != ProductConstants.DeletedStatus)
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);

            if (includedIds.Count == 0)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "At least one included variant is required before publish.",
                    [new ApplicationFieldError("variantConfiguration", "At least one included variant is required.")]);
            }

            var pricedIds = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.PriceListId == defaultPriceList.Id &&
                            x.ProductId == product.Id &&
                            x.Status == "ACTIVE" &&
                            x.ProductVariantId.HasValue &&
                            includedIds.Contains(x.ProductVariantId.Value) &&
                            x.SellingPrice > 0)
                .Select(x => x.ProductVariantId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var missing = includedIds.Except(pricedIds).ToList();
            if (missing.Count > 0)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Every included sellable variant must have a selling price before publish.",
                    [new ApplicationFieldError(
                        "pricingTax.variantPrices",
                        $"{missing.Count} included variant(s) are missing a valid selling price.")]);
            }

            return null;
        }

        var hasPrice = await _dbContext.PriceListItems
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId &&
                           x.PriceListId == defaultPriceList.Id &&
                           x.ProductId == product.Id &&
                           x.Status == "ACTIVE" &&
                           x.SellingPrice > 0,
                cancellationToken);

        if (!hasPrice)
        {
            return new ApplicationError(
                "product.validation_failed",
                "Standard selling price is required before publish.",
                [new ApplicationFieldError("pricingTax.standardSellingPrice", "Standard selling price is required.")]);
        }

        return null;
    }

    public async Task SaveVariantsAsync(
        Guid tenantId,
        Guid productId,
        VariantConfigurationDto variantConfiguration,
        CancellationToken cancellationToken)
    {
        // Prefer Local: wizard-create adds Product in the same transaction before SaveChanges.
        var product = _dbContext.Products.Local
                .FirstOrDefault(p => p.Id == productId && p.TenantId == tenantId)
            ?? await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId, cancellationToken);

        if (product == null) return;
        
        var existingOptions = await _dbContext.ProductOptions
            .Where(o => o.TenantId == tenantId && o.ProductId == productId)
            .ToListAsync(cancellationToken);
            
        var existingValues = await _dbContext.ProductOptionValues
            .Where(v => v.TenantId == tenantId && existingOptions.Select(o => o.Id).Contains(v.ProductOptionId))
            .ToListAsync(cancellationToken);

        var existingVariants = await _dbContext.ProductVariants
            .Where(v => v.TenantId == tenantId && v.ProductId == productId)
            .ToListAsync(cancellationToken);

        var existingVariantValues = await _dbContext.ProductVariantOptionValues
            .Where(v => v.TenantId == tenantId && v.ProductId == productId)
            .ToListAsync(cancellationToken);
        
        var now = DateTimeOffset.UtcNow;
        var optionValueMap = new Dictionary<string, Guid>(); // key: OptionName|ValueName, value: ProductOptionValueId
        var optionIdByValueKey = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var currentOptionIds = new List<Guid>();

        // 1. Process Options and Option Values
        foreach (var optionDto in variantConfiguration.Options)
        {
            var optionId = optionDto.ProductOptionId ?? Guid.NewGuid();
            var existingOption = existingOptions.FirstOrDefault(o => o.Id == optionId || o.OptionCode == optionDto.OptionCode);
            
            if (existingOption == null)
            {
                existingOption = ProductOption.Create(
                    optionId, tenantId, productId, optionDto.SourceOptionTemplateId,
                    optionDto.OptionCode, optionDto.OptionName, optionDto.OptionType,
                    optionDto.InputType ?? "TEXT", true, optionDto.SortOrder, "ACTIVE", null, now);
                _dbContext.ProductOptions.Add(existingOption);
            }
            else
            {
                optionId = existingOption.Id;
                existingOption.UpdateProfile(optionDto.OptionCode, optionDto.OptionName, optionDto.OptionType, optionDto.InputType ?? "TEXT", true, optionDto.SortOrder, null, now);
                _dbContext.ProductOptions.Update(existingOption);
            }

            currentOptionIds.Add(optionId);

            foreach (var valueDto in optionDto.Values)
            {
                var valId = valueDto.ProductOptionValueId ?? Guid.NewGuid();
                var existingVal = existingValues.FirstOrDefault(v => v.Id == valId || (v.ProductOptionId == optionId && v.ValueName == valueDto.ValueName));
                
                if (existingVal == null)
                {
                    existingVal = ProductOptionValue.Create(
                        valId, tenantId, optionId, valueDto.SourceOptionTemplateValueId,
                        valueDto.ValueCode, valueDto.ValueName, valueDto.DisplayName,
                        valueDto.ColorHex, valueDto.ImageMediaAssetId, valueDto.SortOrder, "ACTIVE", null, now);
                    _dbContext.ProductOptionValues.Add(existingVal);
                }
                else
                {
                    valId = existingVal.Id;
                    existingVal.UpdateProfile(valueDto.ValueCode, valueDto.ValueName, valueDto.DisplayName, valueDto.ColorHex, valueDto.SortOrder, null, now);
                    existingVal.AssignImage(valueDto.ImageMediaAssetId, null, now);
                    _dbContext.ProductOptionValues.Update(existingVal);
                }

                var valueKey = $"{optionDto.OptionName.ToLowerInvariant()}|{valueDto.ValueName.ToLowerInvariant()}";
                optionValueMap[valueKey] = valId;
                optionIdByValueKey[valueKey] = optionId;
            }
        }

        // 2. Process Variants and Mappings
        var currentVariantIds = new List<Guid>();
        var currentValueIds = optionValueMap.Values.ToHashSet();

        foreach (var variantDto in variantConfiguration.Variants)
        {
            var orderedSelected = variantDto.SelectedValues
                .OrderBy(x => x.SourceOptionTemplateId?.ToString("D") ?? x.OptionName ?? string.Empty,
                    StringComparer.Ordinal)
                .ToList();

            var legacyPreviewHash = ProductVariantCombinationHashHelper.GenerateLegacyMd5PreviewHash(
                orderedSelected.Select(x => (x.SourceOptionTemplateId, x.SourceOptionTemplateValueId, x.OptionName, x.ValueName)));

            var clientKey = !string.IsNullOrWhiteSpace(variantDto.ClientCombinationKey)
                ? variantDto.ClientCombinationKey
                : BuildClientKeyFromSelectedValues(orderedSelected);

            var resolvedPairs = ResolveVariantOptionValuePairs(
                orderedSelected,
                optionValueMap,
                optionIdByValueKey,
                existingValues);

            var canonicalHash = resolvedPairs.Count > 0
                ? ProductVariantCombinationHashHelper.GenerateCanonicalHash(resolvedPairs)
                : null;

            var existingVariant = existingVariants.FirstOrDefault(v =>
                (variantDto.ProductVariantId.HasValue && v.Id == variantDto.ProductVariantId.Value) ||
                ProductVariantCombinationHashHelper.MatchesCombinationHash(
                    v.OptionCombinationHash,
                    canonicalHash,
                    legacyPreviewHash) ||
                ProductVariantCombinationHashHelper.MatchesCombinationHash(
                    v.OptionCombinationHash,
                    variantDto.OptionCombinationHash,
                    legacyPreviewHash));

            var variantId = variantDto.ProductVariantId ?? existingVariant?.Id ?? Guid.NewGuid();

            if (existingVariant == null)
            {
                existingVariant = ProductVariant.Create(
                    id: variantId,
                    tenantId: tenantId,
                    productId: productId,
                    variantCode: variantDto.VariantCode ?? Guid.NewGuid().ToString().Substring(0, 8),
                    variantName: variantDto.DisplayLabel ?? variantDto.CombinationLabel ?? "Variant",
                    sku: null,
                    stockUomId: Guid.Empty,
                    salesUomId: Guid.Empty,
                    isDefaultVariant: false,
                    isSellable: variantDto.Included,
                    allowFractionalQuantity: false,
                    status: variantDto.Status ?? "ACTIVE",
                    createdByTenantUserId: null,
                    now: now
                );
                _dbContext.ProductVariants.Add(existingVariant);
            }
            else
            {
                variantId = existingVariant.Id;
                existingVariant.UpdateDisplayLabel(variantDto.DisplayLabel ?? variantDto.CombinationLabel ?? "Variant", null, now);
                existingVariant.UpdateInclusion(variantDto.Included, null, now);
                existingVariant.UpdateStatus(variantDto.Status ?? "ACTIVE", null, now);
                _dbContext.ProductVariants.Update(existingVariant);

                if (!variantDto.Included)
                {
                    await SoftDeleteVariantDownstreamPricingAsync(tenantId, productId, variantId, now, cancellationToken);
                }
            }

            if (!string.IsNullOrWhiteSpace(canonicalHash))
            {
                existingVariant.SetOptionCombinationHash(canonicalHash, now);
            }

            currentVariantIds.Add(variantId);

            foreach (var selVal in orderedSelected)
            {
                if (selVal.OptionName == null || selVal.ValueName == null)
                {
                    continue;
                }

                var key = $"{selVal.OptionName.ToLowerInvariant()}|{selVal.ValueName.ToLowerInvariant()}";
                if (!optionValueMap.TryGetValue(key, out var valId))
                {
                    continue;
                }

                var mapping = existingVariantValues.FirstOrDefault(m =>
                    m.ProductVariantId == variantId && m.ProductOptionValueId == valId);
                if (mapping != null)
                {
                    continue;
                }

                var optId = optionIdByValueKey.TryGetValue(key, out var mappedOptionId)
                    ? mappedOptionId
                    : (_dbContext.ProductOptionValues.Local.FirstOrDefault(v => v.Id == valId)?.ProductOptionId
                       ?? existingValues.FirstOrDefault(v => v.Id == valId)?.ProductOptionId
                       ?? Guid.Empty);

                if (optId == Guid.Empty)
                {
                    continue;
                }

                var newMapping = ProductVariantOptionValue.Create(
                    Guid.NewGuid(), tenantId, productId, variantId, optId, valId, null, now);
                _dbContext.ProductVariantOptionValues.Add(newMapping);
            }
        }

        var variantsToRemove = existingVariants
            .Where(v => !currentVariantIds.Contains(v.Id) &&
                        v.Status != ProductConstants.ArchivedStatus)
            .ToList();
        foreach (var variant in variantsToRemove)
        {
            await SoftDeleteVariantDownstreamPricingAsync(tenantId, productId, variant.Id, now, cancellationToken);
            variant.UpdateStatus(ProductConstants.ArchivedStatus, null, now);
            _dbContext.ProductVariants.Update(variant);
        }

        var valuesToRemove = existingValues
            .Where(v => !currentValueIds.Contains(v.Id) &&
                        v.Status != ProductConstants.ArchivedStatus)
            .ToList();
        foreach (var value in valuesToRemove)
        {
            value.UpdateStatus(ProductConstants.ArchivedStatus, null, now);
            _dbContext.ProductOptionValues.Update(value);
        }

        var optionsToRemove = existingOptions
            .Where(o => !currentOptionIds.Contains(o.Id) &&
                        o.Status != ProductConstants.ArchivedStatus)
            .ToList();
        foreach (var option in optionsToRemove)
        {
            option.UpdateStatus(ProductConstants.ArchivedStatus, null, now);
            _dbContext.ProductOptions.Update(option);
        }
    }

    private static string BuildClientKeyFromSelectedValues(
        IReadOnlyList<VariantConfigurationSelectedValueDto> orderedSelected)
    {
        var templatePairs = orderedSelected
            .Where(v => v.SourceOptionTemplateId.HasValue && v.SourceOptionTemplateValueId.HasValue)
            .Select(v => (v.SourceOptionTemplateId!.Value, v.SourceOptionTemplateValueId!.Value))
            .ToList();

        if (templatePairs.Count == orderedSelected.Count && templatePairs.Count > 0)
        {
            return ProductVariantClientKeyHelper.GenerateClientCombinationKey(templatePairs);
        }

        return string.Join(";", orderedSelected.Select(v =>
            $"{v.SourceOptionTemplateId?.ToString("D") ?? v.OptionName}:{v.SourceOptionTemplateValueId?.ToString("D") ?? v.ValueName}"));
    }

    private static List<(Guid ProductOptionId, Guid ProductOptionValueId)> ResolveVariantOptionValuePairs(
        IReadOnlyList<VariantConfigurationSelectedValueDto> orderedSelected,
        Dictionary<string, Guid> optionValueMap,
        Dictionary<string, Guid> optionIdByValueKey,
        List<ProductOptionValue> existingValues)
    {
        var pairs = new List<(Guid, Guid)>();
        foreach (var selVal in orderedSelected)
        {
            if (selVal.OptionName == null || selVal.ValueName == null)
            {
                continue;
            }

            var key = $"{selVal.OptionName.ToLowerInvariant()}|{selVal.ValueName.ToLowerInvariant()}";
            if (!optionValueMap.TryGetValue(key, out var valId))
            {
                continue;
            }

            var optId = optionIdByValueKey.TryGetValue(key, out var mappedOptionId)
                ? mappedOptionId
                : existingValues.FirstOrDefault(v => v.Id == valId)?.ProductOptionId ?? Guid.Empty;

            if (optId != Guid.Empty)
            {
                pairs.Add((optId, valId));
            }
        }

        return pairs;
    }
}
