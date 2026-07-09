namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record OpenTillRequest(
    Guid DeviceId,
    Guid TillId,
    decimal OpeningFloat,
    string? OpeningNote);
