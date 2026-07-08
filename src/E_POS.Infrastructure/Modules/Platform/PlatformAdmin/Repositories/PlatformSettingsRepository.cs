using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformSettingsRepository : IPlatformSettingsRepository
{
    private static readonly IReadOnlyDictionary<string, string> SettingDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PlatformSettingKeys.PlatformDisplayName] = "Platform display name shown in admin surfaces.",
            [PlatformSettingKeys.SupportEmail] = "Platform support contact email.",
            [PlatformSettingKeys.DefaultCountryCode] = "Default ISO country code for new tenant defaults.",
            [PlatformSettingKeys.DefaultCurrencyCode] = "Default ISO currency code for new tenant defaults.",
            [PlatformSettingKeys.DefaultTimezone] = "Default IANA timezone for new tenant defaults.",
            [PlatformSettingKeys.DefaultLocale] = "Default locale tag for new tenant defaults."
        };

    private readonly EPosDbContext _dbContext;

    public PlatformSettingsRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformSettingsResponse> GetGeneralSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlatformSettings
            .AsNoTracking()
            .Where(setting => PlatformSettingKeys.GeneralSettings.Contains(setting.SettingKey))
            .ToListAsync(cancellationToken);

        return MapResponse(settings);
    }

    public async Task<PlatformSettingsResponse> SaveGeneralSettingsAsync(
        UpdatePlatformSettingsRequest request,
        Guid updatedByPlatformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [PlatformSettingKeys.PlatformDisplayName] = NormalizeOptionalText(request.PlatformDisplayName),
            [PlatformSettingKeys.SupportEmail] = NormalizeOptionalText(request.SupportEmail),
            [PlatformSettingKeys.DefaultCountryCode] = NormalizeOptionalText(request.DefaultCountryCode)?.ToUpperInvariant(),
            [PlatformSettingKeys.DefaultCurrencyCode] = NormalizeOptionalText(request.DefaultCurrencyCode)?.ToUpperInvariant(),
            [PlatformSettingKeys.DefaultTimezone] = NormalizeOptionalText(request.DefaultTimezone),
            [PlatformSettingKeys.DefaultLocale] = NormalizeOptionalText(request.DefaultLocale)
        };

        var existingSettings = await _dbContext.PlatformSettings
            .Where(setting => PlatformSettingKeys.GeneralSettings.Contains(setting.SettingKey))
            .ToDictionaryAsync(setting => setting.SettingKey, cancellationToken);

        foreach (var (settingKey, settingValue) in values)
        {
            if (existingSettings.TryGetValue(settingKey, out var existingSetting))
            {
                existingSetting.UpdateValue(settingValue, updatedByPlatformUserId, now);
                continue;
            }

            var newSetting = PlatformSetting.Create(
                Guid.NewGuid(),
                settingKey,
                settingValue,
                isSecret: false,
                SettingDescriptions.GetValueOrDefault(settingKey),
                now);
            newSetting.UpdateValue(settingValue, updatedByPlatformUserId, now);
            _dbContext.PlatformSettings.Add(newSetting);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedSettings = await _dbContext.PlatformSettings
            .AsNoTracking()
            .Where(setting => PlatformSettingKeys.GeneralSettings.Contains(setting.SettingKey))
            .ToListAsync(cancellationToken);

        return MapResponse(savedSettings);
    }

    private static PlatformSettingsResponse MapResponse(IReadOnlyList<PlatformSetting> settings)
    {
        var byKey = settings.ToDictionary(setting => setting.SettingKey, StringComparer.Ordinal);
        var latestUpdatedAt = settings.Count == 0
            ? DateTimeOffset.MinValue
            : settings.Max(setting => setting.UpdatedAt);
        var latestUpdatedBy = settings
            .OrderByDescending(setting => setting.UpdatedAt)
            .Select(setting => setting.UpdatedByPlatformUserId)
            .FirstOrDefault();

        return new PlatformSettingsResponse
        {
            PlatformDisplayName = GetValue(byKey, PlatformSettingKeys.PlatformDisplayName),
            SupportEmail = GetValue(byKey, PlatformSettingKeys.SupportEmail),
            DefaultCountryCode = GetValue(byKey, PlatformSettingKeys.DefaultCountryCode),
            DefaultCurrencyCode = GetValue(byKey, PlatformSettingKeys.DefaultCurrencyCode),
            DefaultTimezone = GetValue(byKey, PlatformSettingKeys.DefaultTimezone),
            DefaultLocale = GetValue(byKey, PlatformSettingKeys.DefaultLocale),
            UpdatedAt = latestUpdatedAt,
            UpdatedByPlatformUserId = latestUpdatedBy
        };
    }

    private static string? GetValue(
        IReadOnlyDictionary<string, PlatformSetting> settings,
        string settingKey)
    {
        return settings.TryGetValue(settingKey, out var setting)
            ? setting.GetStringValue()
            : null;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}



