namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record TillDeviceAssignmentListResponse(
    Guid TillId,
    IReadOnlyList<TillDeviceAssignmentResponse> Items);