using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReceiptRepository
{
    Task<PosReceiptPrintRepositoryResult> RecordPrintAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid saleId,
        PosReceiptPrintRequestDto request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosReceiptPrintRepositoryResult(
    string? ErrorCode,
    PosReceiptPrintResponseDto? Print)
{
    public bool IsSuccess => ErrorCode is null && Print is not null;
}
