namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

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
    DateTimeOffset AssignedAt,
    DateTimeOffset? ReleasedAt);
