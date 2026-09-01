using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed partial class TenantAdminProductRepository
{
    private async Task<ProductSetupInitialTracking> UpsertInitialTrackingAsync(
        Guid tenantId,
        Guid productId,
        Guid userId,
        string? batchNumber,
        DateOnly? expiryDate,
        string? serialNumber,
        Guid? assignedVariantId,
        bool confirmClear,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var row = await _dbContext.ProductSetupInitialTrackings
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.ProductId == productId,
                cancellationToken);

        var normalizedBatch = ProductSetupInitialTrackingRules.NormalizeBatch(batchNumber);
        var normalizedSerial = ProductSetupInitialTrackingRules.NormalizeSerial(serialNumber);

        if (row is null)
        {
            row = ProductSetupInitialTracking.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                normalizedBatch,
                expiryDate,
                normalizedSerial,
                userId,
                now);
            await _dbContext.ProductSetupInitialTrackings.AddAsync(row, cancellationToken);
        }
        else if (row.ConsumedAt is null)
        {
            row.UpdateValues(normalizedBatch, expiryDate, normalizedSerial, userId, now);
        }

        if (confirmClear)
        {
            row.ConfirmIncompatibleClear(userId, now);
        }

        if (assignedVariantId.HasValue)
        {
            row.AssignVariant(assignedVariantId, userId, now);
        }

        return row;
    }

    private async Task<(string? Batch, DateOnly? Expiry, string? Serial, Guid? AssignedVariantId)>
        LoadInitialTrackingValuesAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken)
    {
        var row = await _dbContext.ProductSetupInitialTrackings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .Select(x => new
            {
                x.InitialBatchNumber,
                x.InitialExpiryDate,
                x.InitialSerialNumber,
                x.AssignedProductVariantId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? (null, null, null, null)
            : (row.InitialBatchNumber, row.InitialExpiryDate, row.InitialSerialNumber, row.AssignedProductVariantId);
    }

    private async Task<ApplicationError?> PublishInitialTrackingIdentityAsync(
        Guid tenantId,
        Guid productId,
        Guid userId,
        string productStructure,
        bool trackInventory,
        bool batchTracking,
        bool expiryTracking,
        bool serialTracking,
        string? batchNumber,
        DateOnly? expiryDate,
        string? serialNumber,
        Guid? assignedVariantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tracking = await _dbContext.ProductSetupInitialTrackings
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.ProductId == productId,
                cancellationToken);

        var batch = ProductSetupInitialTrackingRules.NormalizeBatch(
            tracking?.InitialBatchNumber ?? batchNumber);
        var expiry = tracking?.InitialExpiryDate ?? expiryDate;
        var serial = ProductSetupInitialTrackingRules.NormalizeSerial(
            tracking?.InitialSerialNumber ?? serialNumber);
        var assignment = tracking?.AssignedProductVariantId ?? assignedVariantId;

        if (!ProductSetupInitialTrackingRules.HasAnyValues(batch, expiry, serial))
        {
            tracking?.MarkConsumed(userId, now);
            return null;
        }

        if (!trackInventory)
        {
            tracking?.MarkConsumed(userId, now);
            return null;
        }

        var structure = (productStructure ?? "SIMPLE").Trim().ToUpperInvariant();
        if (structure == ProductStructureConstants.Bundle)
        {
            return new ApplicationError(
                ProductSetupInitialTrackingRules.BundleParentNotSupported,
                "Bundle parent products cannot publish Initial Batch or Serial identity.");
        }

        if (expiry.HasValue && string.IsNullOrWhiteSpace(batch))
        {
            return new ApplicationError(
                ProductSetupInitialTrackingRules.BatchRequiredForExpiry,
                "Expiry date requires an Initial Batch Number.");
        }

        Guid? variantId = null;
        if (structure == ProductStructureConstants.Variant)
        {
            var resolved = await ResolveAssignedVariantAsync(
                tenantId,
                productId,
                assignment,
                cancellationToken);
            if (resolved.Error is not null)
            {
                return resolved.Error;
            }

            variantId = resolved.VariantId;
        }

        if (serialTracking && !string.IsNullOrWhiteSpace(serial))
        {
            var serialError = await CreateInitialSerialAsync(
                tenantId,
                productId,
                variantId,
                serial,
                userId,
                now,
                cancellationToken);
            if (serialError is not null)
            {
                return serialError;
            }
        }
        else if (batchTracking && !string.IsNullOrWhiteSpace(batch))
        {
            var persistExpiry = expiryTracking ? expiry : null;
            var batchError = await CreateInitialBatchAsync(
                tenantId,
                productId,
                variantId,
                batch,
                persistExpiry,
                userId,
                now,
                cancellationToken);
            if (batchError is not null)
            {
                return batchError;
            }
        }

        if (tracking is not null)
        {
            tracking.MarkConsumed(userId, now);
        }

        return null;
    }

    private async Task<(Guid? VariantId, ApplicationError? Error)> ResolveAssignedVariantAsync(
        Guid tenantId,
        Guid productId,
        Guid? assignedVariantId,
        CancellationToken cancellationToken)
    {
        var candidates = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.IsSellable &&
                x.Status != ProductConstants.DeletedStatus &&
                x.Status != ProductConstants.ArchivedStatus)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (assignedVariantId.HasValue)
        {
            if (!candidates.Contains(assignedVariantId.Value))
            {
                return (null, new ApplicationError(
                    ProductSetupInitialTrackingRules.InvalidVariantAssignment,
                    "Assigned variant is invalid for Initial Tracking."));
            }

            return (assignedVariantId.Value, null);
        }

        if (candidates.Count == 1)
        {
            return (candidates[0], null);
        }

        return (null, new ApplicationError(
            ProductSetupInitialTrackingRules.VariantAssignmentRequired,
            "Select a sellable included variant for Initial Tracking."));
    }

    private async Task<ApplicationError?> CreateInitialBatchAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        string batchNumber,
        DateOnly? expiryDate,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.ProductBatches
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.ProductVariantId == variantId &&
                     x.BatchNumber == batchNumber,
                cancellationToken);

        if (duplicate)
        {
            return new ApplicationError(
                ProductSetupInitialTrackingRules.DuplicateBatch,
                "A batch with this number already exists for the product.");
        }

        var batch = ProductBatch.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            variantId,
            batchNumber,
            supplierBatchNumber: null,
            manufacturedAt: null,
            expiryDate,
            firstReceivedAt: null,
            ProductConstants.IdentityWithoutStockStatus,
            userId,
            now);

        await _dbContext.ProductBatches.AddAsync(batch, cancellationToken);
        return null;
    }

    private async Task<ApplicationError?> CreateInitialSerialAsync(
        Guid tenantId,
        Guid productId,
        Guid? variantId,
        string serialNumber,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.SerialNumbers
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.ProductId == productId &&
                     x.SerialNumberValue == serialNumber,
                cancellationToken);

        if (duplicate)
        {
            return new ApplicationError(
                ProductSetupInitialTrackingRules.DuplicateSerial,
                "A serial number with this value already exists for the product.");
        }

        var serial = SerialNumber.Create(
            Guid.NewGuid(),
            tenantId,
            productId,
            variantId,
            productBatchId: null,
            currentInventoryBalanceId: null,
            serialNumber,
            ProductConstants.IdentityWithoutStockStatus,
            receivedAt: null,
            userId,
            now);

        await _dbContext.SerialNumbers.AddAsync(serial, cancellationToken);
        return null;
    }
}
