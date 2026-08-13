namespace E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

public sealed record PosCashDrawerSummaryDto(
    Guid TillSessionId,
    Guid TillId,
    string TillName,
    string Status,
    string CurrencyCode,
    decimal OpeningCash,
    decimal CashSales,
    decimal CashRefunds,
    decimal CashIn,
    decimal CashOut,
    decimal CashDrops,
    decimal CurrentExpectedCash,
    string OpenedBy,
    DateTimeOffset OpenedAt);

public sealed record PosCashDrawerMovementDto(
    Guid MovementId,
    string MovementType,
    string Direction,
    decimal Amount,
    string CurrencyCode,
    string? Reason,
    string? Reference,
    string PerformedBy,
    DateTimeOffset PerformedAt);

public sealed record PosCashDrawerMovementPageDto(
    IReadOnlyList<PosCashDrawerMovementDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CreatePosCashMovementRequest(
    Guid RequestId,
    Guid DeviceId,
    Guid TillSessionId,
    string MovementType,
    decimal Amount,
    string Reason,
    string? ReferenceNumber = null);
