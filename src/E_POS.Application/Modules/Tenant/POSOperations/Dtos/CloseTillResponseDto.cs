namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record CloseTillResponseDto(
    ClosedTillSessionDto TillSession);

public sealed record ClosedTillSessionDto(
    Guid Id,
    Guid OutletId,
    Guid TillId,
    decimal OpeningFloat,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal CashDifference,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    string? ClosingNote);
