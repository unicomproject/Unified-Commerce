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
    DateTimeOffset PerformedAt,
    string? MovementNumber = null,
    Guid? MovementTypeId = null,
    string? MovementTypeName = null,
    decimal? CurrentExpectedCash = null);

public sealed record PosCashMovementTypeDto(
    Guid MovementTypeId,
    string Code,
    string Name,
    string Direction,
    bool RequiresReason,
    bool AffectsExpectedCash);

public sealed record PosCashDrawerMovementPageDto(
    IReadOnlyList<PosCashDrawerMovementDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CreatePosCashMovementRequest(
    Guid RequestId,
    Guid DeviceId,
    Guid MovementTypeId,
    decimal Amount,
    string? Note = null);
