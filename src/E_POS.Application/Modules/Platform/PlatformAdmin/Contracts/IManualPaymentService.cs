using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IManualPaymentService
{
    Task<ApplicationResult<ManualPaymentStatusResponse>> GetStatusAsync(string accessToken, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentStatusResponse>> GetInvoiceAsync(string accessToken, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentSubmissionResponse>> SubmitAsync(string accessToken, SubmitManualPaymentEvidenceRequest request,
        ManualPaymentEvidenceUpload upload, string idempotencyKey, Guid correlationId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentSubmissionResponse>> UpdateAsync(string accessToken, Guid paymentId,
        UpdateManualPaymentSubmissionRequest request, ManualPaymentEvidenceUpload upload, string idempotencyKey,
        Guid correlationId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentReviewHistoryResponse>> GetRecipientHistoryAsync(string accessToken, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentQueueResponse>> GetQueueAsync(ManualPaymentQueueQuery query, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentDetailResponse>> GetTenantPaymentStatusAsync(Guid tenantId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentDetailResponse>> GetDetailAsync(Guid paymentId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentProofDownload>> OpenProofAsync(Guid paymentId, Guid evidenceId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentReviewResponse>> ReviewAsync(Guid paymentId, ManualPaymentReviewRequest request,
        string idempotencyKey, Guid correlationId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentReviewHistoryResponse>> GetAdminHistoryAsync(Guid paymentId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<ManualPaymentNotificationResponse>> ResendNotificationAsync(Guid paymentId,
        ResendPaymentNotificationRequest request, string idempotencyKey, Guid correlationId,
        Guid platformUserId, CancellationToken cancellationToken);
}
