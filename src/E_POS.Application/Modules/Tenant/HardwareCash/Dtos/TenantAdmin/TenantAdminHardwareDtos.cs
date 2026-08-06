namespace E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;

public sealed record TenantAdminHardwareDeviceListItemResponse(
    Guid HardwareDeviceId,
    string HardwareDeviceCode,
    string HardwareDeviceName,
    string HardwareDeviceType,
    string ConnectionType,
    string Status,
    Guid OutletId,
    string OutletName,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    DateTimeOffset? LastSeenAt,
    bool IsAssigned,
    Guid? AssignedTillId,
    Guid? AssignedPosDeviceId);

public sealed record TenantAdminHardwareDeviceListResponse(
    IReadOnlyList<TenantAdminHardwareDeviceListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record TenantAdminHardwareDeviceDetailResponse(
    Guid HardwareDeviceId,
    string HardwareDeviceCode,
    string HardwareDeviceName,
    string HardwareDeviceType,
    string ConnectionType,
    string Status,
    Guid OutletId,
    string OutletName,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    string? AssetTag,
    string? FirmwareVersion,
    string? ConfigJson,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsAssigned,
    Guid? ActiveAssignmentId,
    Guid? AssignedTillId,
    Guid? AssignedPosDeviceId);

public sealed record TenantAdminHardwareDeviceCreateRequest(
    Guid OutletId,
    string HardwareDeviceCode,
    string HardwareDeviceName,
    string HardwareDeviceType,
    string ConnectionType,
    string Status = "ACTIVE",
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    string? AssetTag = null,
    string? FirmwareVersion = null,
    string? ConfigJson = null);

public sealed record TenantAdminHardwareAssignmentRequest(
    Guid HardwareDeviceId,
    bool IsPrimary = false);

public sealed record TenantAdminHardwareAssignmentResponse(
    Guid AssignmentId,
    Guid HardwareDeviceId,
    Guid OutletId,
    Guid? TillId,
    Guid? PosDeviceId,
    bool IsPrimary,
    DateTimeOffset AssignedAt);

public sealed record TenantAdminHardwareAssignmentReleaseRequest(
    string? Reason = null);

public sealed record PosHardwareHeartbeatRequest(
    DateTimeOffset? ObservedAt,
    IReadOnlyList<PosHardwareHeartbeatItemRequest> Hardware);

public sealed record PosHardwareHeartbeatItemRequest(
    Guid HardwareDeviceId,
    string? ConnectionStatus = null,
    string? HealthStatus = null,
    string? WarningCode = null,
    string? WarningMessage = null);

public sealed record PosHardwareHeartbeatResponse(
    Guid PosDeviceId,
    DateTimeOffset ServerTime,
    int UpdatedCount);

public sealed record PosHardwareTestResultRequest(
    Guid HardwareDeviceId,
    string TestType,
    string TestStatus,
    string? ResultCode = null,
    string? ResultMessage = null,
    DateTimeOffset? TestedAt = null,
    Guid? PosDeviceId = null);

public sealed record PosHardwareTestResultResponse(
    Guid TestLogId,
    Guid HardwareDeviceId,
    string TestType,
    string TestStatus,
    DateTimeOffset TestedAt);
