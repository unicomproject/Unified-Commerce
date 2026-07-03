namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record TillDeviceAssignmentResponse(
    Guid Id,
    Guid TillId,
    string TillCode,
    string TillName,
    Guid PosDeviceId,
    string DeviceCode,
    string DeviceName,
    Guid OutletId,
    string OutletCode,
    string OutletName,
    string EffectiveFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);