using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.InventoryMaintenance;

public sealed class DevelopmentInventoryTopUpService
{
    public const string ReasonCode = "DEVELOPMENT_STOCK_TOP_UP";
    public const string ReasonName = "Development stock top-up to minimum 100";
    private const string ActiveStatus = "ACTIVE";
    private const string PostedStatus = "POSTED";
    private const string AdjustmentReferenceType = "STOCK_ADJUSTMENT";

    private readonly EPosDbContext _dbContext;

    public DevelopmentInventoryTopUpService(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DevelopmentInventoryContext>> InspectAsync(
        CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .OrderBy(x => x.TenantCode)
            .ToListAsync(cancellationToken);
        var result = new List<DevelopmentInventoryContext>();

        foreach (var tenant in tenants)
        {
            var outlets = await _dbContext.Outlets
                .AsNoTracking()
                .Where(x => x.TenantId == tenant.Id)
                .OrderByDescending(x => x.IsDefaultOutlet)
                .ThenBy(x => x.OutletCode)
                .ToListAsync(cancellationToken);

            foreach (var outlet in outlets)
            {
                var device = await _dbContext.PosDevices
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenant.Id &&
                        x.OutletId == outlet.Id &&
                        x.Status == ActiveStatus)
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => new { x.Id, x.DeviceName })
                    .FirstOrDefaultAsync(cancellationToken);
                var assignedTillId = device is null
                    ? null
                    : await _dbContext.TillDeviceAssignments
                        .AsNoTracking()
                        .Where(x =>
                            x.TenantId == tenant.Id &&
                            x.OutletId == outlet.Id &&
                            x.PosDeviceId == device.Id &&
                            x.ReleasedAt == null)
                        .OrderByDescending(x => x.AssignedAt)
                        .Select(x => (Guid?)x.TillId)
                        .FirstOrDefaultAsync(cancellationToken);
                var locations = await _dbContext.InventoryLocations
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenant.Id &&
                        x.OutletId == outlet.Id &&
                        x.Status == ActiveStatus)
                    .OrderByDescending(x => x.IsSellableLocation)
                    .ThenByDescending(x => x.IsReceivingLocation)
                    .ThenBy(x => x.LocationCode)
                    .Select(x => new
                    {
                        x.Id,
                        x.LocationCode,
                        x.LocationName,
                        x.Status,
                        x.IsSellableLocation,
                        x.IsReceivingLocation,
                    })
                    .ToListAsync(cancellationToken);
                var locationIds = locations.Select(x => x.Id).ToList();
                var balanceStats = await _dbContext.InventoryBalances
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenant.Id &&
                        locationIds.Contains(x.InventoryLocationId))
                    .GroupBy(x => x.InventoryLocationId)
                    .Select(group => new
                    {
                        LocationId = group.Key,
                        Count = group.Count(),
                        Total = group.Sum(x => x.OnHandQuantity),
                    })
                    .ToDictionaryAsync(x => x.LocationId, cancellationToken);
                var locationResults = locations
                    .Select(x =>
                    {
                        balanceStats.TryGetValue(x.Id, out var stats);
                        return new DevelopmentInventoryLocation(
                            x.Id,
                            x.LocationCode,
                            x.LocationName,
                            x.Status,
                            x.IsSellableLocation,
                            x.IsReceivingLocation,
                            stats?.Count ?? 0,
                            stats?.Total ?? 0);
                    })
                    .ToList();
                var users = await _dbContext.TenantUsers
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenant.Id && x.AccountStatus == ActiveStatus)
                    .OrderBy(x => x.Email)
                    .Select(x => x.Email)
                    .ToListAsync(cancellationToken);

                result.Add(new DevelopmentInventoryContext(
                    tenant.Id,
                    tenant.TenantCode,
                    tenant.DisplayName,
                    tenant.Status,
                    outlet.Id,
                    outlet.OutletCode,
                    outlet.OutletName,
                    outlet.Status,
                    device?.Id,
                    device?.DeviceName,
                    assignedTillId,
                    locationResults,
                    users));
            }
        }

        return result;
    }

    public async Task<DevelopmentInventoryTopUpResult> ExecuteAsync(
        DevelopmentInventoryTopUpOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.TenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutletCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.LocationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ActorEmail);
        _ = DevelopmentInventoryTopUpPolicy.CalculateQuantityChange(0, options.TargetMinimum);

        var tenantCode = options.TenantCode.Trim().ToUpperInvariant();
        var outletCode = options.OutletCode.Trim().ToUpperInvariant();
        var locationCode = options.LocationCode.Trim().ToUpperInvariant();
        var actorEmail = options.ActorEmail.Trim().ToUpperInvariant();

        var tenant = await _dbContext.Tenants.SingleOrDefaultAsync(
            x =>
                x.TenantCode.ToUpper() == tenantCode &&
                x.Status.ToUpper() == ActiveStatus,
            cancellationToken)
            ?? throw new InvalidOperationException($"Active tenant '{tenantCode}' was not found.");
        var outlet = await _dbContext.Outlets.SingleOrDefaultAsync(
            x =>
                x.TenantId == tenant.Id &&
                x.OutletCode.ToUpper() == outletCode &&
                x.Status == ActiveStatus,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Active outlet '{outletCode}' was not found for tenant '{tenantCode}'.");
        var location = await _dbContext.InventoryLocations.SingleOrDefaultAsync(
            x =>
                x.TenantId == tenant.Id &&
                x.OutletId == outlet.Id &&
                x.LocationCode.ToUpper() == locationCode &&
                x.Status == ActiveStatus &&
                x.IsSellableLocation,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Active sellable location '{locationCode}' was not found for outlet '{outletCode}'.");
        var actor = await _dbContext.TenantUsers.SingleOrDefaultAsync(
            x =>
                x.TenantId == tenant.Id &&
                x.Email.ToUpper() == actorEmail &&
                x.AccountStatus == ActiveStatus,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Active tenant user '{options.ActorEmail}' was not found for tenant '{tenantCode}'.");

        var products = await _dbContext.Products
            .Where(x =>
                x.TenantId == tenant.Id &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .OrderBy(x => x.ProductCode)
            .ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.Id).ToList();
        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenant.Id,
            productIds,
            cancellationToken);
        products = products.Where(x => !hiddenProductIds.Contains(x.Id)).ToList();
        productIds = products.Select(x => x.Id).ToList();

        var variants = await _dbContext.ProductVariants
            .Where(x =>
                x.TenantId == tenant.Id &&
                productIds.Contains(x.ProductId) &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.VariantCode)
            .ToListAsync(cancellationToken);
        var settings = await _dbContext.ProductInventorySettings
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenant.Id &&
                productIds.Contains(x.ProductId) &&
                x.Status == ActiveStatus)
            .ToListAsync(cancellationToken);
        var balances = await _dbContext.InventoryBalances
            .Where(x =>
                x.TenantId == tenant.Id &&
                x.InventoryLocationId == location.Id &&
                productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        var productById = products.ToDictionary(x => x.Id);
        var items = new List<DevelopmentInventoryTopUpItem>();
        var candidates = new List<TopUpCandidate>();
        var skippedNonStockTracked = 0;
        var skippedBatchTracked = 0;
        var skippedSerialTracked = 0;
        var alreadySufficient = 0;

        foreach (var variant in variants)
        {
            var product = productById[variant.ProductId];
            var setting = settings.FirstOrDefault(x => x.ProductVariantId == variant.Id)
                ?? settings.FirstOrDefault(x =>
                    x.ProductId == product.Id &&
                    x.ProductVariantId == null);
            var isStockTracked = setting?.IsStockTracked ?? true;
            var requiresBatch = setting?.RequiresBatchTracking ?? false;
            var requiresSerial = setting?.RequiresSerialTracking ?? false;

            if (!isStockTracked)
            {
                skippedNonStockTracked++;
                items.Add(CreateSkipped(product, variant, "SKIPPED_NON_STOCK_TRACKED"));
                continue;
            }

            if (requiresSerial)
            {
                skippedSerialTracked++;
                items.Add(CreateSkipped(product, variant, "SKIPPED_SERIAL_TRACKED"));
                continue;
            }

            if (requiresBatch)
            {
                skippedBatchTracked++;
                items.Add(CreateSkipped(product, variant, "SKIPPED_BATCH_DETAILS_REQUIRED"));
                continue;
            }

            var balance = balances.SingleOrDefault(x =>
                x.ProductId == product.Id &&
                x.ProductVariantId == variant.Id &&
                x.ProductBatchId == null);
            var before = balance?.OnHandQuantity ?? 0;
            var change = DevelopmentInventoryTopUpPolicy.CalculateQuantityChange(
                before,
                options.TargetMinimum);

            if (change == 0)
            {
                alreadySufficient++;
                items.Add(new DevelopmentInventoryTopUpItem(
                    product.Id,
                    product.ProductCode,
                    product.ProductName,
                    variant.Id,
                    variant.VariantCode,
                    variant.VariantName,
                    before,
                    0,
                    before,
                    balance?.Id,
                    null,
                    null,
                    "ALREADY_SUFFICIENT"));
                continue;
            }

            candidates.Add(new TopUpCandidate(product, variant, balance, before, change));
        }

        if (candidates.Count == 0)
        {
            return BuildResult(
                tenant.Id, tenant.TenantCode, outlet.Id, outlet.OutletCode,
                location.Id, location.LocationCode, actor.Id, actor.Email,
                options.TargetMinimum, products.Count, variants.Count,
                alreadySufficient, skippedNonStockTracked, skippedBatchTracked,
                skippedSerialTracked, 0, null, null, items);
        }

        var now = DateTimeOffset.UtcNow;
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var missingBalancesCreated = 0;
        var adjustment = StockAdjustment.Create(
            Guid.NewGuid(),
            tenant.Id,
            $"DEV-TOPUP-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..45],
            PostedStatus,
            actor.Id,
            now);

        try
        {
            var reason = await _dbContext.StockAdjustmentReasons.SingleOrDefaultAsync(
                x => x.TenantId == tenant.Id && x.ReasonCode == ReasonCode,
                cancellationToken);
            if (reason is null)
            {
                reason = StockAdjustmentReason.Create(
                    Guid.NewGuid(), tenant.Id, ReasonCode, ReasonName, "INCREASE",
                    false, true, ActiveStatus, actor.Id, now);
                await _dbContext.StockAdjustmentReasons.AddAsync(reason, cancellationToken);
            }
            else
            {
                reason.Update(ReasonName, false, ActiveStatus, actor.Id, now);
            }

            await _dbContext.StockAdjustments.AddAsync(adjustment, cancellationToken);

            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var balance = candidate.Balance;
                if (balance is null)
                {
                    balance = InventoryBalance.Create(
                        Guid.NewGuid(), tenant.Id, location.Id,
                        candidate.Product.Id, candidate.Variant.Id, null, now);
                    await _dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
                    missingBalancesCreated++;
                }

                var line = StockAdjustmentLine.Create(
                    Guid.NewGuid(), tenant.Id, adjustment.Id, index + 1,
                    candidate.Product.Id, candidate.Variant.Id, null,
                    candidate.QuantityBefore, candidate.QuantityChange, null,
                    $"{ReasonCode}: {ReasonName}", now);
                await _dbContext.StockAdjustmentLines.AddAsync(line, cancellationToken);

                var movement = StockMovement.Create(
                    Guid.NewGuid(), tenant.Id,
                    $"DEV-SM-{now:yyyyMMddHHmmssfff}-{index + 1:D4}-{Guid.NewGuid():N}"[..55],
                    balance.Id, StockMovementConstants.Adjustment,
                    candidate.QuantityBefore, candidate.QuantityChange,
                    null, null, ReasonCode, adjustment.AdjustmentNumber,
                    $"DEV-TOPUP:{tenant.Id:N}:{location.Id:N}:{candidate.Variant.Id:N}:{options.TargetMinimum}:{candidate.QuantityBefore}:{balance.RowVersion}",
                    ReasonName, now, actor.Id, now);
                await _dbContext.StockMovements.AddAsync(movement, cancellationToken);

                var movementReference = StockMovementReference.Create(
                    Guid.NewGuid(), tenant.Id, movement.Id,
                    AdjustmentReferenceType, adjustment.Id, line.Id, now);
                await _dbContext.StockMovementReferences.AddAsync(
                    movementReference, cancellationToken);

                balance.AdjustQuantities(candidate.QuantityChange, 0, 0, 0, now);
                items.Add(new DevelopmentInventoryTopUpItem(
                    candidate.Product.Id,
                    candidate.Product.ProductCode,
                    candidate.Product.ProductName,
                    candidate.Variant.Id,
                    candidate.Variant.VariantCode,
                    candidate.Variant.VariantName,
                    candidate.QuantityBefore,
                    candidate.QuantityChange,
                    balance.OnHandQuantity,
                    balance.Id,
                    line.Id,
                    movement.Id,
                    "TOPPED_UP"));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return BuildResult(
            tenant.Id, tenant.TenantCode, outlet.Id, outlet.OutletCode,
            location.Id, location.LocationCode, actor.Id, actor.Email,
            options.TargetMinimum, products.Count, variants.Count,
            alreadySufficient, skippedNonStockTracked, skippedBatchTracked,
            skippedSerialTracked, missingBalancesCreated, adjustment.Id,
            adjustment.AdjustmentNumber, items);
    }

    private async Task<HashSet<Guid>> ResolveHiddenProductIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var posChannelId = await (
                from channel in _dbContext.SalesChannels.AsNoTracking()
                join platformChannel in _dbContext.PlatformSalesChannels.AsNoTracking()
                    on channel.PlatformSalesChannelId equals platformChannel.Id
                where channel.TenantId == tenantId &&
                      channel.Status == ActiveStatus &&
                      (platformChannel.ChannelCode.ToUpper() == "POS" ||
                       platformChannel.ChannelType.ToUpper() == "PHYSICAL")
                orderby channel.SortOrder
                select (Guid?)channel.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!posChannelId.HasValue)
        {
            return [];
        }

        var rows = await _dbContext.ProductChannelVisibilities
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var hidden = new HashSet<Guid>();
        foreach (var productId in rows.Select(x => x.ProductId).Distinct())
        {
            var productRows = rows
                .Where(x => x.ProductId == productId && x.SalesChannelId == posChannelId.Value)
                .ToList();
            if (productRows.Count > 0 &&
                !productRows.Any(x => x.IsVisible && x.Status == ActiveStatus))
            {
                hidden.Add(productId);
            }
        }

        return hidden;
    }

    private static DevelopmentInventoryTopUpItem CreateSkipped(
        E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.Product product,
        E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant variant,
        string outcome) =>
        new(
            product.Id, product.ProductCode, product.ProductName,
            variant.Id, variant.VariantCode, variant.VariantName,
            0, 0, 0, null, null, null, outcome);

    private static DevelopmentInventoryTopUpResult BuildResult(
        Guid tenantId,
        string tenantCode,
        Guid outletId,
        string outletCode,
        Guid locationId,
        string locationCode,
        Guid actorId,
        string actorEmail,
        decimal targetMinimum,
        int productsInspected,
        int variantsInspected,
        int alreadySufficient,
        int skippedNonStockTracked,
        int skippedBatchTracked,
        int skippedSerialTracked,
        int missingBalancesCreated,
        Guid? adjustmentId,
        string? adjustmentNumber,
        IReadOnlyList<DevelopmentInventoryTopUpItem> items) =>
        new(
            tenantId, tenantCode, outletId, outletCode, locationId, locationCode,
            actorId, actorEmail, targetMinimum, productsInspected,
            variantsInspected, items.Count(x => x.Outcome == "TOPPED_UP"),
            alreadySufficient, skippedNonStockTracked, skippedBatchTracked,
            skippedSerialTracked, missingBalancesCreated, adjustmentId,
            adjustmentNumber, ReasonCode,
            items.Count(x => x.StockAdjustmentLineId.HasValue),
            items.Count(x => x.StockMovementId.HasValue),
            items.OrderBy(x => x.ProductCode).ThenBy(x => x.VariantCode).ToList());

    private sealed record TopUpCandidate(
        E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.Product Product,
        E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductVariant Variant,
        InventoryBalance? Balance,
        decimal QuantityBefore,
        decimal QuantityChange);
}
