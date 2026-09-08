using System.Text.Json.Nodes;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontBrandingRepository : IStorefrontBrandingRepository
{
    private const string ActiveStatus = "ACTIVE";
    private const string OnlineChannelCode = "ONLINE";
    private const string LogoPurpose = "ONLINE_STORE_LOGO";
    private const string FaviconPurpose = "ONLINE_STORE_FAVICON";
    private readonly EPosDbContext _dbContext;
    private readonly IMediaReadUrlResolver? _mediaReadUrlResolver;

    public StorefrontBrandingRepository(
        EPosDbContext dbContext,
        IMediaReadUrlResolver? mediaReadUrlResolver = null)
    {
        _dbContext = dbContext;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    public async Task<StorefrontBrandingReadModel?> GetBrandingAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Set<TenantEntity>()
            .AsNoTracking()
            .Where(item => item.Id == tenantId && item.Status == TenantStatusConstants.Active)
            .Select(item => new { item.Id, item.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var channelName = await (
                from channel in _dbContext.SalesChannels.AsNoTracking()
                join platformChannel in _dbContext.PlatformSalesChannels.AsNoTracking()
                    on channel.PlatformSalesChannelId equals platformChannel.Id
                where channel.TenantId == tenantId &&
                      platformChannel.ChannelCode == OnlineChannelCode
                select channel.CustomName)
            .FirstOrDefaultAsync(cancellationToken);

        var rawSettings = await (
                from setting in _dbContext.TenantSettings.AsNoTracking()
                join definition in _dbContext.SettingDefinitions.AsNoTracking()
                    on setting.SettingDefinitionId equals definition.Id
                where setting.TenantId == tenantId &&
                      definition.SettingKey == TenantSettingKeys.OnlineStoreDefaults
                select setting.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);

        var settings = ParseObject(rawSettings);
        var branding = settings?["branding"] as JsonObject;
        var logoMediaAssetId = ReadGuid(branding, "logoMediaAssetId");
        var faviconMediaAssetId = ReadGuid(branding, "faviconMediaAssetId");
        var mediaIds = new[] { logoMediaAssetId, faviconMediaAssetId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var media = await _dbContext.Set<MediaAsset>()
            .AsNoTracking()
            .Where(asset => asset.TenantId == tenantId &&
                            mediaIds.Contains(asset.Id) &&
                            asset.Status == ActiveStatus)
            .Select(asset => new BrandingMediaRow(
                asset.Id,
                asset.AssetPurpose,
                asset.ContainerName,
                asset.StorageKey,
                asset.PublicUrl))
            .ToDictionaryAsync(asset => asset.Id, cancellationToken);

        return new StorefrontBrandingReadModel
        {
            TenantId = tenant.Id,
            StoreName = ReadString(settings, "businessDisplayName") ??
                        channelName ??
                        tenant.DisplayName,
            StoreDescription = ReadString(settings, "storeDescription"),
            LogoImageUrl = ResolveMediaUrl(logoMediaAssetId, LogoPurpose, media),
            FaviconImageUrl = ResolveMediaUrl(faviconMediaAssetId, FaviconPurpose, media),
            PrimaryColor = ReadColor(branding, "primaryColor", "#FF6A00"),
            SecondaryColor = ReadColor(branding, "secondaryColor", "#000000")
        };
    }

    private string? ResolveMediaUrl(
        Guid? mediaAssetId,
        string expectedPurpose,
        IReadOnlyDictionary<Guid, BrandingMediaRow> media)
    {
        if (!mediaAssetId.HasValue ||
            !media.TryGetValue(mediaAssetId.Value, out var asset) ||
            !string.Equals(asset.AssetPurpose, expectedPurpose, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return _mediaReadUrlResolver?.ResolveReadUrl(
                   asset.ContainerName,
                   asset.StorageKey,
                   asset.PublicUrl) ??
               asset.PublicUrl?.Trim();
    }

    private static JsonObject? ParseObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(value) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonObject? source, string key)
    {
        if (source?[key] is not JsonValue value ||
            !value.TryGetValue<string>(out var result) ||
            string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        return result.Trim();
    }

    private static Guid? ReadGuid(JsonObject? source, string key) =>
        Guid.TryParse(ReadString(source, key), out var value) ? value : null;

    private static string ReadColor(JsonObject? source, string key, string fallback)
    {
        var value = ReadString(source, key);
        return value is { Length: 7 } && value[0] == '#' &&
               value.Skip(1).All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : fallback;
    }

    private sealed record BrandingMediaRow(
        Guid Id,
        string AssetPurpose,
        string ContainerName,
        string StorageKey,
        string? PublicUrl);
}
