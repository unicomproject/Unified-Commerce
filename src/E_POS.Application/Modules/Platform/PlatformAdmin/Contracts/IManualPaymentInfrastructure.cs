using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IManualPaymentAccessTokenService
{
    string GenerateToken();
    string HashToken(string rawToken);
}

public interface IInvitationTokenService
{
    string GenerateToken();
    string HashToken(string rawToken);
}

public interface IManualPaymentEvidenceStorage
{
    bool IsConfigured { get; }
    Task<ManualPaymentStoredObject> UploadAsync(Guid tenantId, Guid paymentId, Guid evidenceId,
        string safeFileName, Stream content, string contentType, IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string container, string storageKey, CancellationToken cancellationToken);
    Task DeleteIfExistsAsync(string container, string storageKey, CancellationToken cancellationToken);
}

public interface IManualPaymentEvidenceScanner
{
    Task<string> ScanAsync(Stream content, string contentType, CancellationToken cancellationToken);
}

public sealed record ManualPaymentStoredObject(string Container, string StorageKey);

public sealed record ManualPaymentAccessContext(
    SubscriptionPaymentLink Access,
    SubscriptionPaymentTransaction Payment,
    SubscriptionInvoice Invoice,
    E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant Tenant,
    TenantSubscription Subscription,
    string PlanName,
    PlatformTenantOnboardingOperation? Operation);

public sealed record ManualPaymentEvidenceWrite(
    Guid Id, string Container, string StorageKey, string OriginalFileName, string SafeFileName,
    string ContentType, long Length, string Sha256, string ScanStatus);

public sealed record ManualPaymentSubmitCommand(
    Guid AccessId, Guid PaymentId, decimal SubmittedAmount, string CurrencyCode, string PaymentMethod,
    string Reference, DateTimeOffset PaymentDate, string? PayerNote, long? ExpectedVersion,
    string IdempotencyKeyHash, string RequestHash, Guid CorrelationId,
    ManualPaymentEvidenceWrite Evidence, DateTimeOffset Now);

public sealed record ManualPaymentReviewCommand(
    Guid PaymentId, string Action, long ExpectedVersion, string? ReviewNote, string? ReasonCode,
    string IdempotencyKeyHash, string RequestHash, Guid CorrelationId, Guid ReviewerId, DateTimeOffset Now);

public enum ManualPaymentMutationOutcome
{
    Success, Replay, NotFound, InvalidAccess, InvalidTransition, ConcurrencyConflict,
    IdempotencyConflict, AmountMismatch, CurrencyMismatch, ProofRequired, ProofNotClean, RateLimited
}

public sealed record ManualPaymentSubmitResult(ManualPaymentMutationOutcome Outcome, ManualPaymentSubmissionResponse? Response = null);
public sealed record ManualPaymentReviewResult(ManualPaymentMutationOutcome Outcome, ManualPaymentReviewResponse? Response = null);
public sealed record ManualPaymentNotificationResult(ManualPaymentMutationOutcome Outcome, ManualPaymentNotificationResponse? Response = null);

public interface IManualPaymentRepository
{
    Task<ManualPaymentAccessContext?> FindAccessAsync(string tokenHash, string action, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAccessAsync(Guid accessId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ManualPaymentStatusResponse?> GetStatusAsync(Guid paymentId, string accessToken, CancellationToken cancellationToken);
    Task<ManualPaymentSubmitResult> SubmitAsync(ManualPaymentSubmitCommand command, CancellationToken cancellationToken);
    Task<ManualPaymentReviewHistoryResponse?> GetHistoryAsync(Guid paymentId, bool includeActor, CancellationToken cancellationToken);
    Task<ManualPaymentQueueResponse> GetQueueAsync(ManualPaymentQueueQuery query, CancellationToken cancellationToken);
    Task<ManualPaymentDetailResponse?> GetDetailAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<SubscriptionPaymentEvidence?> GetEvidenceAsync(Guid paymentId, Guid evidenceId, CancellationToken cancellationToken);
    Task RecordProofAccessAsync(Guid paymentId, Guid evidenceId, Guid actorId, Guid correlationId,
        DateTimeOffset now, CancellationToken cancellationToken);
    Task<ManualPaymentReviewResult> ReviewAsync(ManualPaymentReviewCommand command, CancellationToken cancellationToken);
    Task<ManualPaymentNotificationResult> ResendNotificationAsync(Guid paymentId, string notificationType,
        string? reason, string idempotencyKeyHash, string requestHash, Guid correlationId,
        Guid actorId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IPaymentProvider
{
    string ProviderType { get; }
    PaymentProviderCapabilities Capabilities { get; }
    Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken);
    Task<PaymentProviderStatus> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken);
    Task<PaymentProviderCallbackResult> VerifyCallbackAsync(PaymentProviderCallbackRequest request, CancellationToken cancellationToken);
    Task CancelAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken);
    Task RefundAsync(string providerPaymentId, decimal amount, string currencyCode, string idempotencyKey, CancellationToken cancellationToken);
    string MapProviderStatus(string providerStatus);
}

public sealed record PaymentProviderCapabilities(bool Checkout, bool Callback, bool Cancel, bool Refund);
public sealed record PaymentSessionRequest(Guid PaymentId, Guid InvoiceId, decimal Amount, string CurrencyCode, string IdempotencyKey);
public sealed record PaymentSessionResult(string? ProviderPaymentId, string? CheckoutUrl, PaymentProviderStatus Status);
public sealed record PaymentProviderStatus(string Status, string? ProviderEventId = null);
public sealed record PaymentProviderCallbackRequest(string Payload, IReadOnlyDictionary<string, string> Headers);
public sealed record PaymentProviderCallbackResult(bool IsValid, string? ProviderPaymentId, string? ProviderEventId, PaymentProviderStatus Status);
