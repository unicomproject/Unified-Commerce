using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Exceptions;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.UnitTests.PlatformAdministration;

/// <summary>
/// Test double that resolves operational defaults without requiring setting_definitions seed.
/// Dedicated Phase 4 provider tests use the real <c>DefaultTenantSettingsProvider</c>.
/// </summary>
internal sealed class PassingDefaultTenantSettingsProvider : IDefaultTenantSettingsProvider
{
    public string? Currency { get; init; } = "LKR";
    public string? Timezone { get; init; } = "Asia/Colombo";
    public string? Locale { get; init; } = "en-LK";
    public IReadOnlyList<TenantSetting> SettingsToInsert { get; init; } = [];
    public Exception? ExceptionToThrow { get; init; }

    public Task<DefaultTenantSettingsProvisionResult> BuildAsync(
        DefaultTenantSettingsProvisionRequest request,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        var currency = Normalize(request.RequestCurrency)
                       ?? Normalize(Currency)
                       ?? Normalize(request.PlanCurrency)
                       ?? throw new MissingPlatformGeneralDefaultException(PlatformSettingKeys.DefaultCurrencyCode);

        var timezone = Normalize(request.RequestTimezone)
                       ?? Normalize(Timezone)
                       ?? throw new MissingPlatformGeneralDefaultException(PlatformSettingKeys.DefaultTimezone);

        var locale = Normalize(request.RequestLocale)
                     ?? Normalize(Locale)
                     ?? throw new MissingPlatformGeneralDefaultException(PlatformSettingKeys.DefaultLocale);

        return Task.FromResult(new DefaultTenantSettingsProvisionResult(
            SettingsToInsert,
            currency,
            timezone,
            locale,
            SettingsToInsert.Select(_ => "test.setting").ToList(),
            []));
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
