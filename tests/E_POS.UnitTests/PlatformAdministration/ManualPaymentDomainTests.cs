using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class ManualPaymentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ManualPayment_HappyPath_UsesDocumentedStateMachine()
    {
        var payment = CreatePayment();

        var before = payment.SubmitManual(125m, "usd", "bank_transfer", "BANK-0001", Now.AddDays(-1),
            "Paid in full", "key", "request", "payment_recipient", null, Now);
        var reviewBefore = payment.BeginReview(Now.AddMinutes(1));
        payment.Approve(Guid.NewGuid(), 125m, "Verified against bank statement", Now.AddMinutes(2));

        Assert.Equal(ManualPaymentConstants.AwaitingPayment, before);
        Assert.Equal(ManualPaymentConstants.PaymentSubmitted, reviewBefore);
        Assert.Equal(ManualPaymentConstants.Paid, payment.TransactionStatus);
        Assert.Equal(125m, payment.ApprovedAmount);
        Assert.Equal(1, payment.SubmissionVersion);
        Assert.Equal(4, payment.Version);
    }

    [Fact]
    public void ActionRequired_AllowsCorrectedResubmission_AndRetainsSubmissionVersion()
    {
        var payment = CreatePayment();
        payment.SubmitManual(125m, "USD", "BANK_TRANSFER", "BANK-0001", Now, null,
            "key-1", "request-1", "PAYMENT_RECIPIENT", null, Now);
        payment.BeginReview(Now.AddMinutes(1));
        payment.RequestInformation(Guid.NewGuid(), "REFERENCE_UNREADABLE", "Upload a clearer receipt.", Now.AddMinutes(2));

        var before = payment.SubmitManual(125m, "USD", "BANK_TRANSFER", "BANK-0002", Now, null,
            "key-2", "request-2", "PAYMENT_RECIPIENT", null, Now.AddMinutes(3));

        Assert.Equal(ManualPaymentConstants.ActionRequired, before);
        Assert.Equal(ManualPaymentConstants.PaymentSubmitted, payment.TransactionStatus);
        Assert.Equal(2, payment.SubmissionVersion);
    }

    [Fact]
    public void ApprovalBeforeSubmission_IsRejected()
    {
        var payment = CreatePayment();
        Assert.Throws<InvalidOperationException>(() => payment.BeginReview(Now));
        Assert.Throws<InvalidOperationException>(() => payment.Approve(Guid.NewGuid(), 125m, null, Now));
    }

    [Fact]
    public void ManualAccess_StoresHashOnly_AndNeverCheckoutUrl()
    {
        var access = SubscriptionPaymentLink.CreateManualAccess(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), new string('a', 64), Now.AddDays(14), Now);

        Assert.Null(access.TokenHash);
        Assert.Null(access.PaymentUrl);
        Assert.False(access.Allows("STATUS", Now));

        access.ProvisionToken(new string('b', 64), "billing@example.test", Now.AddMinutes(1));

        Assert.Equal(new string('b', 64), access.TokenHash);
        Assert.Null(access.PaymentUrl);
        Assert.True(access.Allows("STATUS", Now.AddMinutes(2)));
        Assert.False(access.Allows("ADMIN_REVIEW", Now.AddMinutes(2)));
    }

    [Fact]
    public async Task ManualProvider_IsProviderNeutralWithoutCheckoutOrCallbacks()
    {
        var provider = new ManualPaymentProvider();
        var session = await provider.CreateSessionAsync(new(Guid.NewGuid(), Guid.NewGuid(), 125m, "USD", "key"),
            CancellationToken.None);

        Assert.Equal("MANUAL", provider.ProviderType);
        Assert.Equal(new(false, false, false, false), provider.Capabilities);
        Assert.Null(session.CheckoutUrl);
        Assert.Null(session.ProviderPaymentId);
        Assert.Equal(ManualPaymentConstants.AwaitingPayment, session.Status.Status);
    }

    private static SubscriptionPaymentTransaction CreatePayment() =>
        SubscriptionPaymentTransaction.CreateAwaitingManual(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 125m, "USD", $"MANUAL-{Guid.NewGuid():N}", Now);
}
