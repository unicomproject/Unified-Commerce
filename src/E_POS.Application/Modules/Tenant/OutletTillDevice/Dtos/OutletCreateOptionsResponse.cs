namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletCreateOptionsResponse(
    IReadOnlyList<OutletLookupOptionResponse> OutletTypes,
    IReadOnlyList<OutletCountryOptionResponse> Countries,
    IReadOnlyList<OutletLookupOptionResponse> Timezones,
    OutletCreateDefaultsResponse Defaults,
    OutletCapacityInfoResponse? Capacity = null);

public sealed record OutletCapacityInfoResponse(
    string LimitKey,
    int CurrentUsage,
    int? EffectiveLimit,
    int? RemainingCapacity,
    bool IsUnlimited,
    bool CanCreate,
    bool OverrideApplied);

public sealed record OutletLookupOptionResponse(string Value, string Label);

public sealed record OutletCountryOptionResponse(string Code, string Name);

public sealed record OutletCreateDefaultsResponse(
    string CountryCode,
    string Timezone,
    string Status);
