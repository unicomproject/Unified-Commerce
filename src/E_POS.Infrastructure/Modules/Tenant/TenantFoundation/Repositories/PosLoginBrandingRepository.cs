using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;

public sealed class PosLoginBrandingRepository : IPosLoginBrandingRepository
{
    private readonly EPosDbContext _dbContext;

    public PosLoginBrandingRepository(EPosDbContext dbContext) => _dbContext = dbContext;

    public Task<PosLoginBrandingTenantSnapshot?> FindActiveTenantBySlugAsync(
        string tenantSlug,
        CancellationToken cancellationToken) =>
        BuildTenantQuery(_dbContext.Tenants.AsNoTracking()
                .Where(x => x.Status.ToUpper() == "ACTIVE" && x.TenantSlug == tenantSlug))
            .Select(x => new PosLoginBrandingTenantSnapshot(
                x.TenantId, x.TenantSlug, x.DisplayName, x.TradingName,
                x.LogoMediaAssetId, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PosLoginBrandingTenantSnapshot?> FindTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        BuildTenantQuery(_dbContext.Tenants.AsNoTracking()
                .Where(x => x.Id == tenantId))
            .Select(x => new PosLoginBrandingTenantSnapshot(
                x.TenantId, x.TenantSlug, x.DisplayName, x.TradingName,
                x.LogoMediaAssetId, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, string>> GetSettingValuesAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await (
            from setting in _dbContext.TenantSettings.AsNoTracking()
            join definition in _dbContext.SettingDefinitions.AsNoTracking()
                on setting.SettingDefinitionId equals definition.Id
            where setting.TenantId == tenantId && definition.Status == "ACTIVE"
            select new { definition.SettingKey, setting.SettingValue })
            .ToDictionaryAsync(x => x.SettingKey, x => x.SettingValue, StringComparer.Ordinal, cancellationToken);

    public Task<PosLoginBrandingMediaSnapshot?> FindMediaAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken) =>
        _dbContext.MediaAssets.AsNoTracking()
            .Where(x => x.Id == mediaAssetId)
            .Select(x => new PosLoginBrandingMediaSnapshot(
                x.Id, x.TenantId, x.PublicUrl, x.MimeType, x.FileExtension,
                x.FileSizeBytes, x.AssetType, x.AssetPurpose, x.Status,
                x.UpdatedAt ?? x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveSettingsAsync(
        Guid tenantId,
        IReadOnlyDictionary<string, string?> values,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var definitions = await _dbContext.SettingDefinitions
            .Where(x => values.Keys.Contains(x.SettingKey) && x.Status == "ACTIVE" && x.IsTenantEditable)
            .ToDictionaryAsync(x => x.SettingKey, StringComparer.Ordinal, cancellationToken);

        if (definitions.Count != values.Count)
            throw new InvalidOperationException("POS login branding setting definitions are unavailable.");

        var definitionIds = definitions.Values.Select(x => x.Id).ToArray();
        var existing = await _dbContext.TenantSettings
            .Where(x => x.TenantId == tenantId && definitionIds.Contains(x.SettingDefinitionId))
            .ToDictionaryAsync(x => x.SettingDefinitionId, cancellationToken);

        foreach (var pair in values)
        {
            var definition = definitions[pair.Key];
            if (pair.Value is null)
            {
                if (existing.TryGetValue(definition.Id, out var remove))
                    _dbContext.TenantSettings.Remove(remove);
                continue;
            }

            if (existing.TryGetValue(definition.Id, out var setting))
                setting.UpdateValue(pair.Value, now);
            else
                _dbContext.TenantSettings.Add(TenantSetting.Create(
                    Guid.NewGuid(), tenantId, definition.Id, pair.Value, null, now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<TenantProjection> BuildTenantQuery(IQueryable<TenantEntity> tenants) =>
        from tenant in tenants
        join profile in _dbContext.TenantProfiles.AsNoTracking()
            on tenant.Id equals profile.TenantId into profiles
        from profile in profiles.DefaultIfEmpty()
        let latestSettingUpdate = _dbContext.TenantSettings
            .Where(x => x.TenantId == tenant.Id)
            .Select(x => (DateTimeOffset?)x.UpdatedAt)
            .Max()
        select new TenantProjection(
            tenant.Id,
            tenant.TenantSlug,
            tenant.DisplayName,
            tenant.Status,
            profile == null ? null : profile.TradingName,
            profile == null ? null : profile.LogoMediaAssetId,
            latestSettingUpdate
                ?? (profile != null
                    ? profile.UpdatedAt ?? profile.CreatedAt
                    : (DateTimeOffset?)null)
                ?? tenant.UpdatedAt
                ?? tenant.CreatedAt);

    private sealed record TenantProjection(
        Guid TenantId,
        string TenantSlug,
        string DisplayName,
        string Status,
        string? TradingName,
        Guid? LogoMediaAssetId,
        DateTimeOffset UpdatedAt);
}
