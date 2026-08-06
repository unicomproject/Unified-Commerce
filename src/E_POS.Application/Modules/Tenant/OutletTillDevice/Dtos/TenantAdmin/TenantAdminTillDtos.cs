namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminTillListItemResponse(
    Guid TillId,
    string TillName,
    string TillCode,
    Guid OutletId,
    string OutletName,
    string Status,
    string? DeviceStatus,
    DateTimeOffset? LastActiveAt,
    bool NeedsAttention,
    string OperationalStatus,
    string DisplayStatus,
    string? CurrentCashierName,
    DateTimeOffset? LastDeviceSeenAt,
    bool HasActiveAssignment);

public sealed record TenantAdminTillListResponse(
    IReadOnlyList<TenantAdminTillListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record TenantAdminTillSummaryResponse(
    int TotalTills,
    int OnlineTills,
    int OfflineTills,
    int InactiveTills,
    int NeedsAttentionTills);

public sealed record TenantAdminTillCreateRequest(
    string TillName,
    string TillCode,
    Guid OutletId,
    string Status,
    string? DeviceName = null,
    string? PrinterName = null,
    string? ScannerName = null,
    string? CashDrawerName = null,
    string? CardReaderName = null,
    string? InternalNote = null);

public sealed record TenantAdminTillUpdateRequest(
    string TillName,
    string TillCode,
    Guid OutletId,
    string Status,
    string? DeviceName = null,
    string? PrinterName = null,
    string? ScannerName = null,
    string? CashDrawerName = null,
    string? CardReaderName = null,
    string? InternalNote = null);

public sealed record TenantAdminTillDetailResponse(
    Guid TillId,
    string TillName,
    string TillCode,
    Guid OutletId,
    string OutletName,
    string OutletCode,
    string Status,
    string? DeviceStatus,
    DateTimeOffset? LastActiveAt,
    bool NeedsAttention,
    string OperationalStatus,
    string DisplayStatus,
    string? CurrentCashierName,
    DateTimeOffset? LastDeviceSeenAt,
    bool HasActiveAssignment,
    string? DeviceName,
    string? PrinterName,
    string? ScannerName,
    string? CashDrawerName,
    string? CardReaderName,
    string? InternalNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantAdminOutletOptionResponse(
    Guid OutletId,
    string OutletName,
    string OutletCode,
    string Status);

public sealed record TenantAdminTillHardwareReadinessResponse(
    Guid TillId,
    string TillName,
    string TillCode,
    Guid OutletId,
    string OutletName,
    IReadOnlyList<TenantAdminHardwareConnectionResponse> Connections,
    string TillStatus,
    string OperationalStatus,
    TenantAdminTillCashierResponse? Cashier,
    DateTimeOffset? LastActivityAt,
    TenantAdminTillPosDeviceResponse? PosDevice,
    IReadOnlyList<TenantAdminTillAttentionReasonResponse> AttentionReasons,
    int AlertCount);

public sealed record TenantAdminTillCashierResponse(
    Guid TenantUserId,
    string DisplayName);

public sealed record TenantAdminTillPosDeviceResponse(
    Guid PosDeviceId,
    string DeviceCode,
    string DeviceName,
    string DeviceStatus,
    bool IsTrusted,
    DateTimeOffset? LastSeenAt);

public sealed record TenantAdminTillAttentionReasonResponse(
    string Code,
    string Severity,
    string Message,
    Guid? HardwareDeviceId,
    string? HardwareType,
    DateTimeOffset? ObservedAt);

public sealed record TenantAdminHardwareConnectionResponse(
    Guid HardwareDeviceId,
    string HardwareDeviceName,
    string HardwareDeviceType,
    string HardwareDeviceCode,
    string OperationalStatus,
    string ConnectionStatus,
    string? LastTestStatus,
    DateTimeOffset? LastTestAt,
    DateTimeOffset? LastSeenAt,
    Guid? AssignmentId = null,
    string? ConnectionType = null,
    string? Manufacturer = null,
    string? Model = null,
    string? HealthStatus = null,
    string? WarningCode = null,
    string? WarningMessage = null,
    bool IsPrimary = false,
    string? AssignmentSource = null);
