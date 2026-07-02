namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletUpdateRequest(
    string Name,
    string OutletCode,
    string Status,
    string OutletType,
    bool IsOnlineVisible,
    string? ContactPhone,
    string? ContactEmail,
    OutletAddressRequest Address,
    IReadOnlyList<OutletBusinessHourRequest>? BusinessHours,
    bool CollectionEnabled);