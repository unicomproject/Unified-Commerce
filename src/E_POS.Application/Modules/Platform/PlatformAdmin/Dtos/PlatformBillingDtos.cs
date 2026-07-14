namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformBillingQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? TenantId = null,
    string? Status = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string DateField = "issuedAt",
    string SortBy = "createdAt",
    string SortDirection = "desc");

public sealed record PlatformBillingCurrencySummaryDto(
    string CurrencyCode,
    decimal PaidRevenue,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int InvoiceCount);

public sealed record PlatformBillingSummaryResponse(
    IReadOnlyList<PlatformBillingCurrencySummaryDto> Currencies,
    int TotalInvoices,
    DateTimeOffset GeneratedAt);

public sealed record PlatformBillingInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid TenantId,
    string TenantCode,
    string TenantName,
    Guid SubscriptionId,
    string SubscriptionStatus,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    string CurrencyCode,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    string StoredStatus,
    string DisplayStatus,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool CanIssue,
    bool CanMarkPaid);

public sealed record PlatformBillingInvoiceListResponse(
    IReadOnlyList<PlatformBillingInvoiceDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PlatformBillingInvoiceLineDto(
    Guid Id,
    string LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LineTotal);

public sealed record PlatformBillingPaymentDto(
    Guid Id,
    string ProviderName,
    string ProviderTransactionId,
    string Status,
    string CurrencyCode,
    decimal Amount,
    decimal ProviderFee,
    decimal NetAmount,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);

public sealed record PlatformBillingInvoiceDetailResponse(
    PlatformBillingInvoiceDto Invoice,
    string InvoiceType,
    string? BillingCycle,
    DateTimeOffset? BillingPeriodStart,
    DateTimeOffset? BillingPeriodEnd,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TaxAmount,
    IReadOnlyList<PlatformBillingInvoiceLineDto> Lines,
    IReadOnlyList<PlatformBillingPaymentDto> Payments);

public sealed record PlatformBillingTenantOptionDto(Guid Id, string Code, string Name);
public sealed record PlatformBillingFilterOptionsResponse(
    IReadOnlyList<PlatformBillingTenantOptionDto> Tenants,
    IReadOnlyList<string> Statuses);

public sealed record PlatformBillingTransitionRequest(DateTimeOffset ExpectedUpdatedAt);
public sealed record PlatformBillingMarkPaidRequest(DateTimeOffset ExpectedUpdatedAt, DateTimeOffset? PaidAt = null);

public enum PlatformBillingMutationOutcome
{
    Success,
    NotFound,
    InvalidTransition,
    ConcurrencyConflict
}

public sealed record PlatformBillingMutationResult(
    PlatformBillingMutationOutcome Outcome,
    PlatformBillingInvoiceDto? Invoice = null);
