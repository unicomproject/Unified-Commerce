namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletResponse(
    Guid Id,
    string OutletCode,
    string Name,
    string Status,
    string OutletType,
    bool IsOnlineVisible,
    string? ContactPhone,
    string? ContactEmail,
    OutletAddressResponse Address,
    IReadOnlyList<OutletBusinessHourResponse> BusinessHours,
    bool CollectionEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);