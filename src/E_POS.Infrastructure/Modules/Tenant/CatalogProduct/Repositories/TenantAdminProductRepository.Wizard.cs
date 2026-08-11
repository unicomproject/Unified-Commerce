using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Platform.PlatformFoundation.Entities;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed partial class TenantAdminProductRepository
{
    private const string DefaultCostingMethod = "WEIGHTED_AVERAGE";
    private const string StagedMediaStatus = "STAGED";

    public Task<bool> ActiveCategoryExistsAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == categoryId &&
                     x.Status == CategoryConstants.ActiveStatus,
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

            if (command.CurrentStage == 8 && !command.IsExplicitDraftSave)
            {
                product.SetPublished(userId, now, command.DesiredPublishStatus);
            }

            var auditAction = command.CurrentStage == 8 && !command.IsExplicitDraftSave
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
                UnitConversions: unitProjection.UnitConversions));
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
            UnitConversions: unitProjection.UnitConversions);
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
        return await (
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
            select new TenantAdminProductImageResponse(
                image.Id,
                image.MediaAssetId,
                image.ProductVariantId,
                media != null ? media.PublicUrl ?? string.Empty : string.Empty,
                image.AltText,
                image.ImagePurpose,
                image.SortOrder,
                image.IsPrimaryImage))
            .ToListAsync(cancellationToken);
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
}
