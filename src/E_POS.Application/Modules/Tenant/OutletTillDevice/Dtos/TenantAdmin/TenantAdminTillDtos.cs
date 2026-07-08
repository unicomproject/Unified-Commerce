namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminTillListItemResponse(
    Guid TillId,
    string TillName,
    string TillCode,
    Guid OutletId,
    string OutletName,
    string Status,
    string DeviceStatus,
    DateTimeOffset? LastActiveAt,
    bool NeedsAttention);

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
    string DeviceStatus,
    DateTimeOffset? LastActiveAt,
    bool NeedsAttention,
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
