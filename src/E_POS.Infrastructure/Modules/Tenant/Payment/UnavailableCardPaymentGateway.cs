using E_POS.Application.Modules.Tenant.Payment.Contracts;

namespace E_POS.Infrastructure.Modules.Tenant.Payment;

/// <summary>
/// Production-safe default until a tenant card provider is configured.
/// It never authorizes, captures, or fabricates provider references.
/// </summary>
public sealed class UnavailableCardPaymentGateway : ICardPaymentGateway
{
    public Task<CardPaymentCaptureResult> CaptureAsync(
        CardPaymentCaptureRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardPaymentCaptureResult(
            CardPaymentOutcome.ProviderUnavailable,
            FailureCode: "card_provider_unavailable"));

    public Task<CardTerminalStatusResult> GetTerminalStatusAsync(
        CardTerminalStatusRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CardTerminalStatusResult(
            CardTerminalStatus.ProviderUnavailable,
            SafeMessage:
                "Card terminal is unavailable. Configure a supported provider and terminal.",
            FailureCode: "card_provider_unavailable",
            CheckedAt: DateTimeOffset.UtcNow));
}
