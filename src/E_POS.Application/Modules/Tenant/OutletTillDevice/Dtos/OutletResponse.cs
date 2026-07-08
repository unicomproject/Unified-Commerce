namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletResponse(
    Guid Id,
    string OutletCode,
    string OutletName,
    string Status,
    string OutletType,
    string Timezone,
    bool IsDefaultOutlet,
    string? Phone,
    string? Email,
    OutletAddressResponse Address,
    IReadOnlyList<OutletBusinessHourResponse> BusinessHours,
    bool CollectionEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
