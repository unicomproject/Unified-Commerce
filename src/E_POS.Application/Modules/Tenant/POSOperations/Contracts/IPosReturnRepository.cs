using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReturnRepository
{
    Task<PosReturnCompleteRepositoryResult> CompleteReturnAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosReturnCompleteCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosReturnCreditPreviewRepositoryResult> PreviewCreditAsync(
        Guid tenantId,
        Guid saleId,
        string reasonCode,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosReturnSaleEligibilityDto?> GetSaleEligibilityAsync(
        Guid tenantId,
        Guid saleId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosReturnSaleSearchPageDto> SearchOriginalSalesAsync(
        Guid tenantId,
        string searchType,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record PosReturnCreditPreviewRepositoryResult(
    PosReturnCreditPreviewDto? Preview,
    string? ErrorCode);

public sealed record PosReturnCompleteCommand(
    Guid SaleId,
    Guid DeviceId,
    Guid TillSessionId,
    Guid OutletId,
    Guid TillId,
    string ReasonCode,
    string SettlementMethodCode,
    string? Notes,
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines);

public sealed record PosReturnCompleteRepositoryResult(
    PosReturnReceiptDto? Receipt,
    string? ErrorCode);
