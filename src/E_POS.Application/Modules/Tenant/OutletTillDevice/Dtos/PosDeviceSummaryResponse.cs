namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record PosDeviceSummaryResponse(
    Guid Id,
    Guid OutletId,
    string OutletCode,
    string OutletName,
    string DeviceCode,
    string Name,
    string DeviceSerialNumber,
    string Status,
    Guid? AssignedTillId,
    string? AssignedTillCode,
    string? AssignedTillName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
