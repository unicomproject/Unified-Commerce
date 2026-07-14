using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformBillingRepository
{
    Task<PlatformBillingSummaryResponse> GetSummaryAsync(PlatformBillingQuery query, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PlatformBillingInvoiceListResponse> GetInvoicesAsync(PlatformBillingQuery query, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PlatformBillingInvoiceDetailResponse?> GetInvoiceAsync(Guid invoiceId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlatformBillingPaymentDto>?> GetPaymentsAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<PlatformBillingFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken);
    Task<PlatformBillingMutationResult> IssueAsync(Guid invoiceId, DateTimeOffset expectedUpdatedAt, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PlatformBillingMutationResult> MarkPaidAsync(Guid invoiceId, DateTimeOffset expectedUpdatedAt, DateTimeOffset paidAt, DateTimeOffset now, CancellationToken cancellationToken);
}
