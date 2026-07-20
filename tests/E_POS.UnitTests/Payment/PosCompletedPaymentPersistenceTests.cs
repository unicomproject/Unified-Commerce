using E_POS.Domain.Modules.Tenant.Payment.Entities;
using Xunit;

namespace E_POS.UnitTests.Payment;

public sealed class PosCompletedPaymentPersistenceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCash_DoesNotFabricateCardMetadata()
    {
        var records = PosCompletedPaymentPersistence.CreateCash(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAY-CASH-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LKR",
            1000m,
            1200m,
            1000m,
            200m,
            "idem-cash",
            "hash",
            Guid.NewGuid(),
            Now);

        Assert.Equal("PAID", records.Payment.PaymentStatus);
        Assert.Null(records.Payment.ExternalReference);
        Assert.Equal("CASH", records.Transaction.ProviderName);
        Assert.Null(records.Transaction.ExternalTransactionReference);
        Assert.Null(records.Transaction.ProviderResponseJson);
        Assert.Equal("SUCCEEDED", records.Transaction.TransactionStatus);
        Assert.Contains("Cash", records.Event.EventNote);
    }

    [Fact]
    public void CreateProviderCapture_PersistsSafeCardTipsAndProviderReference()
    {
        var paymentId = Guid.NewGuid();
        var records = PosCompletedPaymentPersistence.CreateProviderCapture(
            paymentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAY-CARD-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LKR",
            2500m,
            2500m,
            "idem-card",
            "hash",
            Guid.NewGuid(),
            Now,
            new PosCompletedPaymentPersistence.ProviderCaptureOutcome(
                "TEST_TERMINAL",
                "txn_abc_001",
                "VISA",
                "4242"));

        Assert.Equal("txn_abc_001", records.Payment.ExternalReference);
        Assert.Equal("txn_abc_001", records.Transaction.ExternalTransactionReference);
        Assert.Equal("TEST_TERMINAL", records.Transaction.ProviderName);
        Assert.Equal("SUCCEEDED", records.Transaction.TransactionStatus);
        Assert.NotNull(records.Transaction.ProviderResponseJson);
        Assert.Contains("4242", records.Transaction.ProviderResponseJson);
        Assert.Contains("Visa", records.Transaction.ProviderResponseJson);
        Assert.DoesNotContain("4111111111114242", records.Transaction.ProviderResponseJson);
        Assert.DoesNotContain("cvv", records.Transaction.ProviderResponseJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "•••• 4242",
            SafePaymentDisplay.FormatMaskedReference(
                SafePaymentDisplay.ResolveLast4(
                    records.Transaction.ProviderResponseJson,
                    records.Payment.ExternalReference)));
    }

    [Fact]
    public void CreateProviderCapture_RejectsInvalidLast4_WithoutPersistingPanLikeValues()
    {
        var records = PosCompletedPaymentPersistence.CreateProviderCapture(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PAY-CARD-2",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LKR",
            100m,
            100m,
            "idem-card-2",
            "hash",
            Guid.NewGuid(),
            Now,
            new PosCompletedPaymentPersistence.ProviderCaptureOutcome(
                "TEST_TERMINAL",
                "txn_no_last4",
                "MASTERCARD",
                "4111111111111288"));

        Assert.Equal("txn_no_last4", records.Payment.ExternalReference);
        var (_, last4) = SafePaymentDisplay.TryParseSanitizedCardMetadata(
            records.Transaction.ProviderResponseJson);
        Assert.Null(last4);
        Assert.DoesNotContain("4111111111111288", records.Transaction.ProviderResponseJson ?? string.Empty);
        Assert.Null(SafePaymentDisplay.FormatMaskedReference(
            SafePaymentDisplay.ResolveLast4(
                records.Transaction.ProviderResponseJson,
                records.Payment.ExternalReference)));
    }
}
