namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record PosDeviceResponse(
    Guid Id,
    Guid OutletId,
    string OutletCode,
    string OutletName,
    string DeviceCode,
    string DeviceName,
    string DeviceType,
    string Status,
    bool IsTrusted,
    Guid? AssignedTillId,
    string? AssignedTillCode,
    string? AssignedTillName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
