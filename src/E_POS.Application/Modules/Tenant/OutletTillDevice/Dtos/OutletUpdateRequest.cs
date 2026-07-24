namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletUpdateRequest(
    string OutletName,
    string Status,
    string OutletType,
    string Timezone,
    bool IsDefaultOutlet,
    string? Phone,
    string? Email,
    OutletAddressRequest Address,
    IReadOnlyList<OutletBusinessHourRequest>? BusinessHours,
    bool CollectionEnabled,
    int? PreparationLeadMinutes = null,
    int? PickupWindowMinutes = null,
    TimeOnly? CollectionCutoffTime = null);
