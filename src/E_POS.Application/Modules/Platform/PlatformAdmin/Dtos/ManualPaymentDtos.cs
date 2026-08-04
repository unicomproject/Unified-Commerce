namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record ManualPaymentStatusResponse(
    Guid TenantId,
    string TenantReference,
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal ExpectedAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string CurrencyCode,
    DateTimeOffset? DueAt,
    string PaymentStatus,
    long Version,
    string PlanName,
    string? BillingCycle,
    string Instructions,
    string InvoiceUrl,
    string PaymentStatusUrl,
    string? CheckoutUrl,
    string TenantStatus,
    string InvitationStatus,
    string TenantName,
    string SubscriptionStatus,
    DateTimeOffset? SubscriptionPeriodStart,
    DateTimeOffset? SubscriptionPeriodEnd,
    string InvoiceStatus,
    decimal SubtotalAmount,
    string? PaymentMethod,
    string? ReferenceSuffix,
    decimal? SubmittedAmount,
    DateTimeOffset? PaymentDate,
    string? PayerNote,
    IReadOnlyList<ManualPaymentEvidenceDto> Evidence,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset UpdatedAt);

public sealed record SubmitManualPaymentEvidenceRequest(
    string PaymentMethod,
    string BankOrTransactionReference,
    decimal SubmittedAmount,
    string CurrencyCode,
    DateTimeOffset PaymentDate,
    string? PayerNote = null,
    long? ExpectedVersion = null);

public sealed record UpdateManualPaymentSubmissionRequest(
    string PaymentMethod,
    string BankOrTransactionReference,
    decimal SubmittedAmount,
    string CurrencyCode,
    DateTimeOffset PaymentDate,
    string? PayerNote,
    long ExpectedVersion);

public sealed record ManualPaymentEvidenceDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    string ScanStatus,
    long SubmissionVersion,
    DateTimeOffset UploadedAt);

public sealed record ManualPaymentSubmissionResponse(
    Guid PaymentId,
    string Status,
    long Version,
    string? ReferenceSuffix,
    decimal ExpectedAmount,
    decimal? SubmittedAmount,
    string CurrencyCode,
    DateTimeOffset? PaymentDate,
    IReadOnlyList<ManualPaymentEvidenceDto> Evidence,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset UpdatedAt,
    string NextAction,
    bool IdempotentReplay = false);

public sealed record ManualPaymentReviewRequest(
    string Action,
    long ExpectedVersion,
    string? ReviewNote = null,
    string? ReasonCode = null);

public sealed record ManualPaymentReviewResponse(
    Guid PaymentId,
    Guid InvoiceId,
    Guid TenantId,
    string PaymentStatus,
    string InvoiceStatus,
    string TenantStatus,
    long Version,
    Guid ReviewId,
    string Result,
    bool ActivationEligible,
    bool IdempotentReplay = false);

public sealed record ManualPaymentReviewHistoryItem(
    Guid Id,
    string Action,
    string StatusBefore,
    string StatusAfter,
    string? ReasonCode,
    string? Note,
    string ActorType,
    Guid? ActorId,
    long PaymentVersion,
    DateTimeOffset CreatedAt);

public sealed record ManualPaymentReviewHistoryResponse(
    Guid PaymentId,
    IReadOnlyList<ManualPaymentReviewHistoryItem> Items);

public sealed record ManualPaymentQueueQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Status = null,
    Guid? TenantId = null,
    string? Search = null,
    Guid? PlanId = null,
    DateTimeOffset? SubmittedFrom = null,
    DateTimeOffset? SubmittedTo = null,
    string SortBy = "submittedAt",
    string SortDirection = "desc");

public sealed record ManualPaymentQueueItem(
    Guid PaymentId,
    Guid TenantId,
    string TenantCode,
    string TenantName,
    string TenantStatus,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid SubscriptionId,
    Guid PlanId,
    string PlanName,
    string? BillingCycle,
    DateTimeOffset? InvoiceDueAt,
    decimal ExpectedAmount,
    decimal? SubmittedAmount,
    string CurrencyCode,
    string Status,
    long Version,
    DateTimeOffset? SubmittedAt,
    long? SubmittedAgeSeconds,
    DateTimeOffset UpdatedAt);

public sealed record ManualPaymentQueueResponse(
    IReadOnlyList<ManualPaymentQueueItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ManualPaymentDetailResponse(
    ManualPaymentQueueItem Payment,
    string? PaymentMethod,
    string? ReferenceSuffix,
    DateTimeOffset? PaymentDate,
    string? PayerNote,
    IReadOnlyList<ManualPaymentEvidenceDto> Evidence,
    IReadOnlyList<ManualPaymentReviewHistoryItem> History,
    IReadOnlyList<string> AllowedActions,
    bool ActivationEligible,
    string SubscriptionStatus,
    string InvoiceStatus,
    decimal SubtotalAmount,
    decimal TaxAmount,
    string InvitationStatus,
    string? SubmittedByType);

public sealed record ResendPaymentNotificationRequest(string NotificationType, string? Reason = null);

public sealed record ManualPaymentNotificationResponse(Guid PaymentId, string NotificationType, string Status, bool IdempotentReplay);

public sealed record ManualPaymentEvidenceUpload(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long Length);

public sealed record ManualPaymentProofDownload(Stream Content, string ContentType, string FileName, long Length);
