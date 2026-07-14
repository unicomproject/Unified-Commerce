using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReturnService
{
    Task<ApplicationResult<PosReturnReceiptDto>> CompleteReturnAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnCompleteRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnCreditPreviewDto>> PreviewCreditAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnCreditPreviewRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnSaleEligibilityDto>> GetSaleEligibilityAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnSaleSearchPageDto>> SearchOriginalSalesAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? searchType,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
