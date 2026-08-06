namespace E_POS.Application.Modules.Tenant.Payment.Contracts;

public enum CardPaymentOutcome
{
    Initiated,
    AwaitingCard,
    Processing,
    Pending,
    Authorized,
    Completed,
    Declined,
    Cancelled,
    Failed,
    Unknown,
    ProviderUnavailable,
    TerminalUnavailable,
    Expired,
    Unavailable
}

public enum CardTerminalStatus
{
    NotConfigured,
    ProviderUnavailable,
    PairingRequired,
    Offline,
    Busy,
    Ready,
    Unknown
}

public sealed record CardPaymentCaptureRequest(
    Guid TenantId,
    Guid TenantUserId,
    Guid DeviceId,
    Guid TillId,
    Guid TillSessionId,
    Guid OperationId,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public sealed record CardPaymentCaptureResult(
    CardPaymentOutcome Outcome,
    string? ProviderName = null,
    string? ProviderTransactionId = null,
    string? CardBrand = null,
    string? CardLast4 = null,
    string? AuthorizationReference = null,
    string? TerminalReference = null,
    string? FailureCode = null);

public sealed record CardTerminalStatusRequest(
    Guid TenantId,
    Guid TenantUserId,
    Guid DeviceId,
    Guid? TillId,
    Guid ConfigurationId,
    int ConfigurationVersion);

public sealed record CardTerminalStatusResult(
    CardTerminalStatus Status,
    string? ProviderName = null,
    string? TerminalReference = null,
    string? SafeMessage = null,
    string? FailureCode = null,
    DateTimeOffset? CheckedAt = null);

public sealed record CardPaymentStatusRequest(
    Guid TenantId,
    Guid TenantUserId,
    Guid DeviceId,
    Guid OperationId);

public sealed record CardPaymentCancelRequest(
    Guid TenantId,
    Guid TenantUserId,
    Guid DeviceId,
    Guid OperationId,
    string IdempotencyKey);

public sealed record CardPaymentReversalRequest(
    Guid TenantId,
    Guid TenantUserId,
    Guid DeviceId,
    Guid TillId,
    Guid TillSessionId,
    Guid OperationId,
    Guid OriginalPaymentId,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public interface ICardPaymentGateway
{
    Task<CardPaymentCaptureResult> CaptureAsync(
        CardPaymentCaptureRequest request,
        CancellationToken cancellationToken);

    Task<CardTerminalStatusResult> GetTerminalStatusAsync(
        CardTerminalStatusRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardTerminalStatusResult(
            CardTerminalStatus.ProviderUnavailable,
            FailureCode: "card_provider_unavailable"));

    Task<CardPaymentCaptureResult> GetPaymentStatusAsync(
        CardPaymentStatusRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardPaymentCaptureResult(
            CardPaymentOutcome.ProviderUnavailable,
            FailureCode: "card_provider_unavailable"));

    Task<CardPaymentCaptureResult> CancelAsync(
        CardPaymentCancelRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardPaymentCaptureResult(
            CardPaymentOutcome.ProviderUnavailable,
            FailureCode: "card_provider_unavailable"));

    Task<CardPaymentCaptureResult> VoidAsync(
        CardPaymentReversalRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardPaymentCaptureResult(
            CardPaymentOutcome.Failed,
            FailureCode: "card_void_not_supported"));

    Task<CardPaymentCaptureResult> RefundAsync(
        CardPaymentReversalRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardPaymentCaptureResult(
            CardPaymentOutcome.Failed,
            FailureCode: "card_refund_not_supported"));
}
