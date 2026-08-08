using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

public sealed record DefaultTenantSettingsProvisionRequest(
    Guid TenantId,
    Guid? PlatformUserId,
    DateTimeOffset Now,
    string? RequestCurrency,
    string? RequestTimezone,
    string? RequestLocale,
    string? PlanCurrency,
    IReadOnlyCollection<string> EffectiveFeatureKeys);

public sealed record DefaultTenantSettingsProvisionResult(
    IReadOnlyList<TenantSetting> SettingsToInsert,
    string ResolvedCurrency,
    string ResolvedTimezone,
    string ResolvedLocale,
    IReadOnlyList<string> ProvisionedSettingKeys,
    IReadOnlyList<string> SkippedEntitlementSettingKeys);
