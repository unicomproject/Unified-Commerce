namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosHomeTimeDto(
    DateTimeOffset ServerNowUtc,
    string OutletTimezone,
    DateOnly? BusinessDate);
