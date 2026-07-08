namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletSummaryResponse(
    Guid Id,
    string OutletCode,
    string OutletName,
    string Status,
    string OutletType,
    string Timezone,
    bool IsDefaultOutlet,
    string? Phone,
    string? Email,
    bool CollectionEnabled);
