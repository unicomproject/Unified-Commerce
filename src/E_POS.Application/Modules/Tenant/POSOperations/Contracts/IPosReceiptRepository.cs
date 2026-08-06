using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReceiptRepository
{
    Task<PosReceiptSearchResponseDto> SearchAsync(
        Guid tenantId,
        PosReceiptSearchRequestDto request,
        CancellationToken cancellationToken);

    Task<PosReceiptDetailDto?> GetDetailAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken);

    Task<PosReceiptReprintAuthorizationResponseDto?> AuthorizeReprintAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid receiptId,
        string reasonCode,
        string? reasonNote,
        DateTimeOffset now,
        CancellationToken cancellationToken);

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
