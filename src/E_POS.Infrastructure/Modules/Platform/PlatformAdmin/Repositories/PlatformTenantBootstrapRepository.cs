using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformTenantBootstrapRepository : IPlatformTenantBootstrapRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformTenantBootstrapRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformTenantBootstrapTenantSnapshot?> GetTenantSnapshotAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await (
            from tenant in _dbContext.Tenants.AsNoTracking()
            where tenant.Id == tenantId
            join subscription in _dbContext.TenantSubscriptions.AsNoTracking()
                on tenant.Id equals subscription.TenantId into subscriptions
            from subscription in subscriptions
                .Where(item => item.Status != "CANCELLED")
                .OrderByDescending(item => item.CreatedAt)
                .Take(1)
                .DefaultIfEmpty()
            join plan in _dbContext.SubscriptionPlans.AsNoTracking()
                on subscription.SubscriptionPlanId equals plan.Id into plans
            from plan in plans.DefaultIfEmpty()
            select new PlatformTenantBootstrapTenantSnapshot(
                tenant.Id,
                tenant.TenantCode,
                tenant.DisplayName,
                tenant.Status,
                plan != null ? plan.PlanName : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PlatformTenantBootstrapFootprintCounts> GetFootprintCountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var activeOutletCount = await _dbContext.Outlets
            .AsNoTracking()
            .CountAsync(
                outlet => outlet.TenantId == tenantId &&
                          outlet.Status == OutletConstants.ActiveStatus,
                cancellationToken);

        var activeTillCount = await _dbContext.Tills
            .AsNoTracking()
            .CountAsync(
                till => till.TenantId == tenantId &&
                        till.Status == OutletConstants.ActiveStatus,
                cancellationToken);

        var customRoleCount = await _dbContext.TenantRoles
            .AsNoTracking()
            .CountAsync(
                role => role.TenantId == tenantId &&
                        role.IsActive &&
                        role.RoleCode != TenantUserConstants.DefaultTenantAdminRoleCode,
                cancellationToken);

        var tenantUserCount = await _dbContext.TenantUsers
            .AsNoTracking()
            .CountAsync(user => user.TenantId == tenantId, cancellationToken);

        var activeOrDraftProductCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(
                product => product.TenantId == tenantId &&
                           product.Status != ProductConstants.DeletedStatus &&
                           product.Status != ProductConstants.InactiveStatus,
                cancellationToken);

        return new PlatformTenantBootstrapFootprintCounts(
            activeOutletCount,
            activeTillCount,
            customRoleCount,
            tenantUserCount,
            activeOrDraftProductCount);
    }

    public Task<bool> OutletBelongsToTenantAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken) =>
        _dbContext.Outlets.AsNoTracking()
            .AnyAsync(outlet => outlet.TenantId == tenantId && outlet.Id == outletId, cancellationToken);

    public Task<bool> RoleBelongsToTenantAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken) =>
        _dbContext.TenantRoles.AsNoTracking()
            .AnyAsync(role => role.TenantId == tenantId && role.Id == roleId, cancellationToken);

    public async Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken)
    {
        if (outletIds.Count == 0)
        {
            return true;
        }

        var count = await _dbContext.Outlets.AsNoTracking()
            .CountAsync(
                outlet => outlet.TenantId == tenantId && outletIds.Contains(outlet.Id),
                cancellationToken);

        return count == outletIds.Count;
    }

    public Task<bool> EmailExistsForTenantAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        _dbContext.TenantUsers.AsNoTracking()
            .AnyAsync(
                user => user.TenantId == tenantId && user.Email == normalizedEmail,
                cancellationToken);

    public async Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var codes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var rows = await _dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission => permission.IsActive && codes.Contains(permission.PermissionCode))
            .Select(permission => new { permission.PermissionCode, permission.Id })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.PermissionCode, row => row.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Guid> CreateCustomRoleAsync(
        Guid tenantId,
        string roleName,
        string? description,
        IReadOnlyList<Guid> permissionIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var roleId = Guid.NewGuid();
        var roleCode = $"CUSTOM_{NormalizeRoleCode(roleName)}_{roleId.ToString("N")[..8].ToUpperInvariant()}";
        var role = TenantRole.Create(
            roleId,
            tenantId,
            null,
            null,
            roleCode,
            roleName.Trim(),
            description?.Trim(),
            isCustom: true,
            isActive: true,
            createdByTenantUserId: null,
            now);

        var rolePermissions = permissionIds
            .Distinct()
            .Select(permissionId => TenantRolePermission.Create(
                Guid.NewGuid(),
                tenantId,
                roleId,
                permissionId,
                grantedByTenantUserId: null,
                now))
            .ToList();

        await _dbContext.TenantRoles.AddAsync(role, cancellationToken);
        await _dbContext.TenantRolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return roleId;
    }

    public async Task<Guid?> ResolveCategoryIdByCodeAsync(
        Guid tenantId,
        string categoryCode,
        CancellationToken cancellationToken) =>
        await _dbContext.Categories.AsNoTracking()
            .Where(category => category.TenantId == tenantId && category.CategoryCode == categoryCode.Trim())
            .Select(category => (Guid?)category.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PlatformTenantBootstrapIdempotencyRecordLookup?> TryGetIdempotencyRecordAsync(
        Guid tenantId,
        string operationType,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var hash = HashIdempotencyKey(idempotencyKey);
        return await _dbContext.PlatformTenantBootstrapIdempotencyRecords
            .AsNoTracking()
            .Where(record =>
                record.TenantId == tenantId &&
                record.OperationType == operationType &&
                record.IdempotencyKeyHash == hash)
            .Select(record => new PlatformTenantBootstrapIdempotencyRecordLookup(
                record.ResponseJson,
                record.RequestHash))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveIdempotencyResponseAsync(
        Guid tenantId,
        string operationType,
        string idempotencyKey,
        string responseJson,
        DateTimeOffset now,
        string? requestHash,
        CancellationToken cancellationToken)
    {
        var record = PlatformTenantBootstrapIdempotencyRecord.Create(
            Guid.NewGuid(),
            tenantId,
            operationType,
            HashIdempotencyKey(idempotencyKey),
            responseJson,
            now,
            requestHash);

        await _dbContext.PlatformTenantBootstrapIdempotencyRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetOnlineStoreDefaultsJsonAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await (
            from setting in _dbContext.TenantSettings.AsNoTracking()
            join definition in _dbContext.SettingDefinitions.AsNoTracking()
                on setting.SettingDefinitionId equals definition.Id
            where setting.TenantId == tenantId &&
                  (definition.SettingKey == TenantSettingKeys.OnlineStoreDefaults ||
                   definition.Id == TenantSettingDefinitionSeed.OnlineStoreDefaultsId)
            select setting.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertOnlineStoreDefaultsAsync(
        Guid tenantId,
        string defaultsJson,
        Guid? platformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var definitionId = await _dbContext.SettingDefinitions
            .AsNoTracking()
            .Where(definition =>
                definition.SettingKey == TenantSettingKeys.OnlineStoreDefaults ||
                definition.Id == TenantSettingDefinitionSeed.OnlineStoreDefaultsId)
            .Select(definition => (Guid?)definition.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? TenantSettingDefinitionSeed.OnlineStoreDefaultsId;

        var existing = await _dbContext.TenantSettings
            .FirstOrDefaultAsync(
                setting => setting.TenantId == tenantId && setting.SettingDefinitionId == definitionId,
                cancellationToken);

        if (existing is not null)
        {
            existing.UpdateValue(defaultsJson, now);
        }
        else
        {
            await _dbContext.TenantSettings.AddAsync(
                TenantSetting.Create(
                    Guid.NewGuid(),
                    tenantId,
                    definitionId,
                    defaultsJson,
                    platformUserId,
                    now),
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasClickCollectCollectionConfiguredAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        (
            from mapping in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
            join method in _dbContext.FulfillmentMethods.AsNoTracking()
                on mapping.FulfillmentMethodId equals method.Id
            join outlet in _dbContext.Outlets.AsNoTracking()
                on mapping.OutletId equals outlet.Id
            where outlet.TenantId == tenantId &&
                  method.TenantId == tenantId &&
                  mapping.Status == OutletConstants.ActiveStatus &&
                  method.Status == OutletConstants.ActiveStatus &&
                  method.MethodType == OutletConstants.PickupMethodType
            select mapping.Id)
            .AnyAsync(cancellationToken);

    public async Task<Guid?> ResolveBrandIdByCodeAsync(
        Guid tenantId,
        string brandCode,
        CancellationToken cancellationToken) =>
        await _dbContext.Brands.AsNoTracking()
            .Where(brand => brand.TenantId == tenantId && brand.BrandCode == brandCode.Trim())
            .Select(brand => (Guid?)brand.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> ResolveOutletIdByCodeAsync(
        Guid tenantId,
        string outletCode,
        CancellationToken cancellationToken) =>
        await _dbContext.Outlets.AsNoTracking()
            .Where(outlet => outlet.TenantId == tenantId && outlet.OutletCode == outletCode.Trim())
            .Select(outlet => (Guid?)outlet.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasInFlightImportBatchAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        _dbContext.PlatformTenantBootstrapProductImportBatches.AsNoTracking()
            .AnyAsync(
                batch => batch.TenantId == tenantId &&
                         (batch.Status == "VALIDATED" || batch.Status == "COMMITTING"),
                cancellationToken);

    public Task<PlatformTenantBootstrapProductImportBatch?> GetImportBatchAsync(
        Guid tenantId,
        Guid importId,
        CancellationToken cancellationToken) =>
        _dbContext.PlatformTenantBootstrapProductImportBatches
            .FirstOrDefaultAsync(batch => batch.TenantId == tenantId && batch.Id == importId, cancellationToken);

    public async Task SaveImportBatchAsync(
        PlatformTenantBootstrapProductImportBatch batch,
        IReadOnlyList<PlatformTenantBootstrapProductImportRow> rows,
        CancellationToken cancellationToken)
    {
        await _dbContext.PlatformTenantBootstrapProductImportBatches.AddAsync(batch, cancellationToken);
        await _dbContext.PlatformTenantBootstrapProductImportRows.AddRangeAsync(rows, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateImportBatchAsync(
        PlatformTenantBootstrapProductImportBatch batch,
        CancellationToken cancellationToken)
    {
        _dbContext.PlatformTenantBootstrapProductImportBatches.Update(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateImportRowsAsync(
        IReadOnlyList<PlatformTenantBootstrapProductImportRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        _dbContext.PlatformTenantBootstrapProductImportRows.UpdateRange(rows);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformTenantBootstrapProductImportRow>> GetImportRowsAsync(
        Guid importId,
        CancellationToken cancellationToken) =>
        await _dbContext.PlatformTenantBootstrapProductImportRows
            .Where(row => row.ImportBatchId == importId)
            .OrderBy(row => row.RowNumber)
            .ToListAsync(cancellationToken);

    private static string NormalizeRoleCode(string roleName)
    {
        var builder = new StringBuilder();
        foreach (var character in roleName.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        var normalized = builder.ToString().Trim('_');
        return normalized.Length == 0 ? "ROLE" : normalized[..Math.Min(normalized.Length, 24)];
    }

    private static string HashIdempotencyKey(string idempotencyKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey.Trim()));
        return Convert.ToHexString(bytes);
    }
}
