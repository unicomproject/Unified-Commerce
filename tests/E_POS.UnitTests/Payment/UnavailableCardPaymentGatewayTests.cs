using E_POS.Application.Modules.Tenant.Payment.Contracts;
using E_POS.Infrastructure.Modules.Tenant.Payment;
using Xunit;

namespace E_POS.UnitTests.Payment;

public sealed class UnavailableCardPaymentGatewayTests
{
    private readonly ICardPaymentGateway _gateway =
        new UnavailableCardPaymentGateway();

    [Fact]
    public async Task Capture_NeverApprovesOrFabricatesReferences()
    {
        var result = await _gateway.CaptureAsync(
            new CardPaymentCaptureRequest(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), 100m, "LKR", "stable-request"),
            CancellationToken.None);

        Assert.Equal(CardPaymentOutcome.ProviderUnavailable, result.Outcome);
        Assert.Equal("card_provider_unavailable", result.FailureCode);
        Assert.Null(result.ProviderTransactionId);
        Assert.Null(result.AuthorizationReference);
        Assert.Null(result.CardLast4);
    }

    [Fact]
    public async Task TerminalStatus_IsAccuratelyProviderUnavailable()
    {
        var result = await _gateway.GetTerminalStatusAsync(
            new CardTerminalStatusRequest(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.Equal(CardTerminalStatus.ProviderUnavailable, result.Status);
        Assert.Equal("card_provider_unavailable", result.FailureCode);
        Assert.Null(result.ProviderName);
        Assert.Null(result.TerminalReference);
    }

    [Fact]
    public async Task RefundAndVoid_AreExplicitlyUnsupported()
    {
        var request = new CardPaymentReversalRequest(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m, "LKR",
            "stable-reversal");

        var refund = await _gateway.RefundAsync(request, CancellationToken.None);
        var voidResult = await _gateway.VoidAsync(request, CancellationToken.None);

        Assert.Equal("card_refund_not_supported", refund.FailureCode);
        Assert.Equal("card_void_not_supported", voidResult.FailureCode);
        Assert.NotEqual(CardPaymentOutcome.Completed, refund.Outcome);
        Assert.NotEqual(CardPaymentOutcome.Completed, voidResult.Outcome);
    }
}
