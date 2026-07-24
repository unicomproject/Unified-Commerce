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

    Task<ApplicationResult<PosReturnReceiptDto>> GetCompletionAsync(
        TenantRequestContext context,
        Guid returnId,
        Guid? deviceId,
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

    Task<ApplicationResult<PosReturnSaleEligibilityDto>> CheckSelectedSaleEligibilityAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnEligibilityCheckRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<PosReturnReasonOptionDto>>> GetReturnReasonsAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnReasonsValidateResponseDto>> ValidateReturnReasonsAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnReasonsValidateRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<PosReturnInspectionConditionDto>>> GetInspectionConditionsAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnInspectionValidateResponseDto>> ValidateInspectionAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnInspectionValidateRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnInspectionDraftResponseDto>> SaveInspectionDraftAsync(
        TenantRequestContext context, Guid saleId, Guid? deviceId,
        PosReturnInspectionDraftSaveRequestDto request, CancellationToken cancellationToken) =>
        Task.FromException<ApplicationResult<PosReturnInspectionDraftResponseDto>>(
            new NotSupportedException("Inspection drafts are not supported by this service."));

    Task<ApplicationResult<PosReturnInspectionDraftResponseDto>> GetInspectionDraftAsync(
        TenantRequestContext context, Guid saleId, Guid? deviceId,
        CancellationToken cancellationToken) =>
        Task.FromException<ApplicationResult<PosReturnInspectionDraftResponseDto>>(
            new NotSupportedException("Inspection drafts are not supported by this service."));

    Task<ApplicationResult<PosReturnInspectionMediaDto>> UploadInspectionMediaAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid saleLineId,
        Guid? deviceId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeleteInspectionMediaAsync(
        TenantRequestContext context,
        Guid mediaId,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnInspectionMediaContentDto>> GetInspectionMediaAsync(
        TenantRequestContext context,
        Guid mediaId,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnSaleSearchPageDto>> SearchOriginalSalesAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? searchType,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? paymentMethodCode,
        decimal? minAmount,
        decimal? maxAmount,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnResolutionResponseDto>> SaveResolutionAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnResolutionSaveRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnResolutionResponseDto>> GetResolutionAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnRefundMethodsResponseDto>> GetRefundMethodsAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosReturnRefundMethodSaveResponseDto>> SaveRefundMethodAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosReturnRefundMethodSaveRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosExchangeProductsResponseDto>> SearchExchangeProductsAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosExchangeReplacementSaveResponseDto>> SaveExchangeReplacementAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosExchangeReplacementSaveRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosExchangeReplacementSaveResponseDto>> GetExchangeReplacementAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosExchangePreviewDto>> PreviewExchangeAsync(
        TenantRequestContext context,
        Guid saleId,
        Guid? deviceId,
        PosExchangePreviewRequestDto request,
        CancellationToken cancellationToken);
}
