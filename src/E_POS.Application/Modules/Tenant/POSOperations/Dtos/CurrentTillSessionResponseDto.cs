namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record CurrentTillSessionResponseDto(
    CurrentTillSessionDto TillSession);

public sealed record CurrentTillSessionDto(
    Guid Id,
    Guid OutletId,
    Guid TillId,
    Guid OpenedDeviceId,
    decimal OpeningFloat,
    string Status,
    DateTimeOffset OpenedAt,
    string? OpeningNote);
