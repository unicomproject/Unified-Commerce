using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Xunit;

namespace E_POS.UnitTests.SubscriptionBilling;

public sealed class SubscriptionBillingDomainPrimitivesTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void PaymentLink_CreatePending_DualWritesLegacyAndSecondBrainColumns()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        const string tokenHash = "sha256:pending-link-token";

        var link = SubscriptionPaymentLink.CreatePending(
            id,
            tenantId,
            invoiceId,
            tokenHash,
            "https://pay.example.test/invoice/abc",
            Now.AddDays(7),
            Now);

        Assert.Equal(id, link.Id);
        Assert.Equal(tenantId, link.TenantId);
        Assert.Equal(invoiceId, link.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, link.InvoiceId);
        Assert.Equal(tokenHash, link.PaymentLinkTokenHash);
        Assert.Equal(tokenHash, link.TokenHash);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentLinkStatusActive, link.LinkStatus);
        Assert.Null(link.SentAt);
        Assert.Null(link.UsedAt);
        Assert.Null(link.RevokedAt);
    }

    [Fact]
    public void PaymentLink_MarkSent_UpdatesSentFields()
    {
        var link = CreatePaymentLink();
        var sentAt = Now.AddMinutes(5);

        link.MarkSent("billing@tenant.test", sentAt);

        Assert.Equal("billing@tenant.test", link.SentToEmail);
        Assert.Equal(sentAt, link.SentAt);
        Assert.Equal(sentAt, link.UpdatedAt);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentLinkStatusActive, link.LinkStatus);
    }

    [Fact]
    public void PaymentLink_MarkUsed_UpdatesStatusAndUsedAt()
    {
        var link = CreatePaymentLink();
        var usedAt = Now.AddHours(1);

        link.MarkUsed(usedAt);

        Assert.Equal(usedAt, link.UsedAt);
        Assert.Equal(usedAt, link.UpdatedAt);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentLinkStatusUsed, link.LinkStatus);
    }

    [Fact]
    public void PaymentLink_Revoke_UpdatesStatusAndRevokedAt()
    {
        var link = CreatePaymentLink();
        var revokedAt = Now.AddHours(2);

        link.Revoke(revokedAt);

        Assert.Equal(revokedAt, link.RevokedAt);
        Assert.Equal(revokedAt, link.UpdatedAt);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentLinkStatusRevoked, link.LinkStatus);
    }

    [Fact]
    public void PaymentTransaction_CreatePending_DualWritesInvoicePaymentLinkAndProviderFields()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentLinkId = Guid.NewGuid();
        const string providerReference = "provider-ref-001";

        var transaction = SubscriptionPaymentTransaction.CreatePending(
            id,
            tenantId,
            invoiceId,
            paymentLinkId,
            120m,
            "LKR",
            "Manual",
            providerReference,
            Now,
            idempotencyKey: "idem-001");

        Assert.Equal(invoiceId, transaction.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, transaction.InvoiceId);
        Assert.Equal(paymentLinkId, transaction.SubscriptionPaymentLinkId);
        Assert.Equal(paymentLinkId, transaction.PaymentLinkId);
        Assert.Equal(providerReference, transaction.ProviderTransactionReference);
        Assert.Equal(providerReference, transaction.ProviderTransactionId);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentTransactionStatusPending, transaction.TransactionStatus);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentTransactionTypePayment, transaction.TransactionType);
        Assert.Equal(120m, transaction.Amount);
        Assert.Equal(120m, transaction.NetAmount);
        Assert.Equal(0m, transaction.ProviderFee);
        Assert.Equal("idem-001", transaction.IdempotencyKey);
    }

    [Fact]
    public void PaymentTransaction_MarkSucceeded_UpdatesStatusPaidAtAndNetAmount()
    {
        var transaction = CreatePaymentTransaction();
        var paidAt = Now.AddMinutes(10);

        transaction.MarkSucceeded(paidAt, providerFee: 2.50m, providerResponseJson: "{\"status\":\"ok\"}");

        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentTransactionStatusSucceeded, transaction.TransactionStatus);
        Assert.Equal(paidAt, transaction.PaidAt);
        Assert.Equal(paidAt, transaction.UpdatedAt);
        Assert.Equal(2.50m, transaction.ProviderFee);
        Assert.Equal(117.50m, transaction.NetAmount);
        Assert.Null(transaction.FailedAt);
        Assert.Null(transaction.FailureReason);
    }

    [Fact]
    public void PaymentTransaction_MarkFailed_UpdatesStatusFailedAtAndFailureReason()
    {
        var transaction = CreatePaymentTransaction();
        var failedAt = Now.AddMinutes(15);

        transaction.MarkFailed(failedAt, "Card declined", providerResponseJson: "{\"status\":\"failed\"}");

        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentTransactionStatusFailed, transaction.TransactionStatus);
        Assert.Equal(failedAt, transaction.FailedAt);
        Assert.Equal(failedAt, transaction.UpdatedAt);
        Assert.Equal("Card declined", transaction.FailureReason);
        Assert.Null(transaction.PaidAt);
    }

    [Fact]
    public void CreditNote_CreateDraft_DualWritesInvoiceAndMoneyFields()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var creditNote = SubscriptionCreditNote.CreateDraft(
            id,
            tenantId,
            invoiceId,
            "CN-001",
            45m,
            "LKR",
            Now,
            reason: "Overcharge adjustment",
            taxAmount: 5m);

        Assert.Equal(invoiceId, creditNote.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, creditNote.InvoiceId);
        Assert.Equal(45m, creditNote.TotalCreditAmount);
        Assert.Equal(45m, creditNote.TotalAmount);
        Assert.Equal(40m, creditNote.SubtotalAmount);
        Assert.Equal(5m, creditNote.TaxAmount);
        Assert.Equal(SubscriptionBillingAlignmentConstants.CreditNoteStatusDraft, creditNote.Status);
        Assert.Equal("Overcharge adjustment", creditNote.Reason);
    }

    [Fact]
    public void CreditNote_Issue_UpdatesStatusAndIssuedAt()
    {
        var creditNote = CreateCreditNote();
        var issuedAt = Now.AddHours(1);

        creditNote.Issue(issuedAt);

        Assert.Equal(SubscriptionBillingAlignmentConstants.CreditNoteStatusIssued, creditNote.Status);
        Assert.Equal(issuedAt, creditNote.IssuedAt);
        Assert.Equal(issuedAt, creditNote.UpdatedAt);
    }

    [Fact]
    public void CreditNote_Apply_UpdatesStatusAndAppliedAt()
    {
        var creditNote = CreateCreditNote();
        creditNote.Issue(Now.AddHours(1));
        var appliedAt = Now.AddHours(2);

        creditNote.Apply(appliedAt);

        Assert.Equal(SubscriptionBillingAlignmentConstants.CreditNoteStatusApplied, creditNote.Status);
        Assert.Equal(appliedAt, creditNote.AppliedAt);
        Assert.Equal(appliedAt, creditNote.UpdatedAt);
    }

    [Fact]
    public void CreditNoteLine_Create_DualWritesLegacyAndSecondBrainLineFields()
    {
        var id = Guid.NewGuid();
        var creditNoteId = Guid.NewGuid();
        var invoiceLineId = Guid.NewGuid();

        var line = SubscriptionCreditNoteLine.Create(
            id,
            creditNoteId,
            "1",
            1,
            "Plan credit",
            1m,
            25m,
            25m,
            Now,
            invoiceLineId: invoiceLineId,
            taxAmount: 0m);

        Assert.Equal(creditNoteId, line.SubscriptionCreditNoteId);
        Assert.Equal(creditNoteId, line.CreditNoteId);
        Assert.Equal(1, line.LineNumberInt);
        Assert.Equal("1", line.LineNumber);
        Assert.Equal(25m, line.LineCreditAmount);
        Assert.Equal(25m, line.LineTotal);
        Assert.Equal(invoiceLineId, line.InvoiceLineId);
    }

    private static SubscriptionPaymentLink CreatePaymentLink() =>
        SubscriptionPaymentLink.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "sha256:test-token",
            "https://pay.example.test/invoice/test",
            Now.AddDays(7),
            Now);

    private static SubscriptionPaymentTransaction CreatePaymentTransaction() =>
        SubscriptionPaymentTransaction.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            "LKR",
            "Manual",
            "provider-ref-test",
            Now);

    private static SubscriptionCreditNote CreateCreditNote() =>
        SubscriptionCreditNote.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CN-TEST",
            30m,
            "LKR",
            Now);
}
