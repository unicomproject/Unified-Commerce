namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillDeviceAssignmentListResponse(
    Guid TillId,
    IReadOnlyList<TillDeviceAssignmentResponse> Items);
