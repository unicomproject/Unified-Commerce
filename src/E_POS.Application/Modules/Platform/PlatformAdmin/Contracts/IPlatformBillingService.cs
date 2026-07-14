using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformBillingService
{
    Task<ApplicationResult<PlatformBillingSummaryResponse>> GetSummaryAsync(PlatformBillingQuery query, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<PlatformBillingInvoiceListResponse>> GetInvoicesAsync(PlatformBillingQuery query, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<PlatformBillingInvoiceDetailResponse>> GetInvoiceAsync(Guid invoiceId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<PlatformBillingPaymentDto>>> GetPaymentsAsync(Guid invoiceId, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<PlatformBillingFilterOptionsResponse>> GetFilterOptionsAsync(Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<PlatformBillingInvoiceDto>> IssueAsync(Guid invoiceId, PlatformBillingTransitionRequest request, Guid platformUserId, CancellationToken cancellationToken);
    Task<ApplicationResult<PlatformBillingInvoiceDto>> MarkPaidAsync(Guid invoiceId, PlatformBillingMarkPaidRequest request, Guid platformUserId, CancellationToken cancellationToken);
}
