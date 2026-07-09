namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record CloseTillRequest(
    Guid DeviceId,
    Guid TillId,
    decimal CountedCash,
    decimal? ExpectedCash,
    string? MismatchReason,
    string? ClosingNote);
