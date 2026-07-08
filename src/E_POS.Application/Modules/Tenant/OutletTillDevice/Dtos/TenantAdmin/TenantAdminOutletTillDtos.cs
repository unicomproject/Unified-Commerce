namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminOutletTillItemResponse(
    Guid TillId,
    string TillName,
    string TillCode,
    string Status,
    decimal? CurrentBalance,
    decimal? OpeningAmount,
    DateTimeOffset? LastOpenedAt,
    DateTimeOffset? LastClosedAt,
    string? AssignedCashierName,
    string DeviceStatus);

public sealed record TenantAdminOutletTillsSummaryResponse(
    int TotalTills,
    int ActiveTills,
    int CurrentlyOpenTills,
    int TillsNeedingAttention);

public sealed record TenantAdminOutletTillsResponse(
    TenantAdminOutletTillsSummaryResponse Summary,
    IReadOnlyList<TenantAdminOutletTillItemResponse> Items);
