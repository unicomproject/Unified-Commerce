using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReceiptService
{
    Task<ApplicationResult<PosReceiptSearchResponseDto>> SearchAsync(
        TenantRequestContext context,
        PosReceiptSearchRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReceiptDetailDto>> GetDetailAsync(
        TenantRequestContext context,
        Guid receiptId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReceiptReprintAuthorizationResponseDto>> AuthorizeReprintAsync(
        TenantRequestContext context,
        Guid receiptId,
        PosReceiptReprintAuthorizationRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReceiptPrintResponseDto>> RecordPrintAsync(
        TenantRequestContext context,
        Guid saleId,
        PosReceiptPrintRequestDto request,
        CancellationToken cancellationToken);
}
