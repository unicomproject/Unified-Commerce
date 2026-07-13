using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class SubscriptionBillingDomainPrimitivesPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 10, 45, 0, TimeSpan.Zero);

    [Fact]
    public async Task PaymentLink_PersistsDualWriteColumnsAndLifecycleTransitions()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        SeedInvoiceGraph(dbContext, tenantId, invoiceId);

        var link = SubscriptionPaymentLink.CreatePending(
            linkId,
            tenantId,
            invoiceId,
            "sha256:persist-link-token",
            "https://pay.example.test/invoice/persist",
            Now.AddDays(7),
            Now);

        dbContext.SubscriptionPaymentLinks.Add(link);
        await dbContext.SaveChangesAsync();

        link.MarkSent("finance@tenant.test", Now.AddMinutes(5));
        link.MarkUsed(Now.AddMinutes(30));
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.SubscriptionPaymentLinks.SingleAsync(x => x.Id == linkId);
        Assert.Equal(invoiceId, saved.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, saved.InvoiceId);
        Assert.Equal("sha256:persist-link-token", saved.PaymentLinkTokenHash);
        Assert.Equal("sha256:persist-link-token", saved.TokenHash);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentLinkStatusUsed, saved.LinkStatus);
        Assert.Equal("finance@tenant.test", saved.SentToEmail);
        Assert.NotNull(saved.SentAt);
        Assert.NotNull(saved.UsedAt);
    }

    [Fact]
    public async Task PaymentTransaction_PersistsDualWriteColumnsAndStatusTransitions()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        SeedInvoiceGraph(dbContext, tenantId, invoiceId);

        var link = SubscriptionPaymentLink.CreatePending(
            linkId,
            tenantId,
            invoiceId,
            "sha256:txn-link-token",
            "https://pay.example.test/invoice/txn",
            Now.AddDays(7),
            Now);
        dbContext.SubscriptionPaymentLinks.Add(link);

        var transaction = SubscriptionPaymentTransaction.CreatePending(
            transactionId,
            tenantId,
            invoiceId,
            linkId,
            99.99m,
            "LKR",
            "Manual",
            "provider-ref-persist",
            Now);
        dbContext.SubscriptionPaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        transaction.MarkSucceeded(Now.AddMinutes(20), providerFee: 1.25m);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.SubscriptionPaymentTransactions.SingleAsync(x => x.Id == transactionId);
        Assert.Equal(invoiceId, saved.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, saved.InvoiceId);
        Assert.Equal(linkId, saved.SubscriptionPaymentLinkId);
        Assert.Equal(linkId, saved.PaymentLinkId);
        Assert.Equal("provider-ref-persist", saved.ProviderTransactionReference);
        Assert.Equal("provider-ref-persist", saved.ProviderTransactionId);
        Assert.Equal(SubscriptionBillingAlignmentConstants.PaymentTransactionStatusSucceeded, saved.TransactionStatus);
        Assert.Equal(98.74m, saved.NetAmount);
        Assert.NotNull(saved.PaidAt);
    }

    [Fact]
    public async Task CreditNoteAndLine_PersistDualWriteColumnsAndLifecycleTransitions()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var creditNoteId = Guid.NewGuid();
        var creditNoteLineId = Guid.NewGuid();

        SeedInvoiceGraph(dbContext, tenantId, invoiceId);

        var creditNote = SubscriptionCreditNote.CreateDraft(
            creditNoteId,
            tenantId,
            invoiceId,
            "CN-PERSIST-001",
            30m,
            "LKR",
            Now,
            reason: "Billing correction",
            taxAmount: 0m);

        var creditNoteLine = SubscriptionCreditNoteLine.Create(
            creditNoteLineId,
            creditNoteId,
            "1",
            1,
            "Plan adjustment",
            1m,
            30m,
            30m,
            Now);

        dbContext.SubscriptionCreditNotes.Add(creditNote);
        dbContext.SubscriptionCreditNoteLines.Add(creditNoteLine);
        await dbContext.SaveChangesAsync();

        creditNote.Issue(Now.AddHours(1));
        creditNote.Apply(Now.AddHours(2));
        await dbContext.SaveChangesAsync();

        var savedCreditNote = await dbContext.SubscriptionCreditNotes.SingleAsync(x => x.Id == creditNoteId);
        var savedLine = await dbContext.SubscriptionCreditNoteLines.SingleAsync(x => x.Id == creditNoteLineId);

        Assert.Equal(invoiceId, savedCreditNote.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, savedCreditNote.InvoiceId);
        Assert.Equal(30m, savedCreditNote.TotalCreditAmount);
        Assert.Equal(30m, savedCreditNote.TotalAmount);
        Assert.Equal(SubscriptionBillingAlignmentConstants.CreditNoteStatusApplied, savedCreditNote.Status);
        Assert.NotNull(savedCreditNote.IssuedAt);
        Assert.NotNull(savedCreditNote.AppliedAt);

        Assert.Equal(creditNoteId, savedLine.SubscriptionCreditNoteId);
        Assert.Equal(creditNoteId, savedLine.CreditNoteId);
        Assert.Equal(30m, savedLine.LineCreditAmount);
        Assert.Equal(30m, savedLine.LineTotal);
        Assert.Equal(savedCreditNote.TotalCreditAmount, savedLine.LineCreditAmount);
    }

    private static void SeedInvoiceGraph(EPosDbContext dbContext, Guid tenantId, Guid invoiceId)
    {
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-BILL-6BC",
            "ten-bill-6bc",
            "Billing Primitive Tenant",
            "pending",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "BILL-6BC",
            "Billing 6BC Plan",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            120m,
            Now,
            baseCurrency: "LKR"));

        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            subscriptionId,
            tenantId,
            planId,
            TenantSubscriptionStatusConstants.Trial,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            trialStartAt: null,
            trialEndAt: null,
            billingStartAt: Now,
            nextBillingAt: Now.AddMonths(1),
            autoRenew: true,
            discountType: null,
            discountValue: null,
            taxPercentage: 0m,
            invoiceEmail: null,
            paymentMethod: null,
            notes: null,
            maxOutletsOverride: null,
            maxTillsOverride: null,
            maxUsersOverride: null,
            currencyCode: "LKR",
            planPrice: 120m,
            startedAt: Now,
            currentPeriodStart: Now,
            currentPeriodEnd: Now.AddMonths(1),
            assignedByPlatformUserId: null,
            createdAt: Now));

        dbContext.SubscriptionInvoices.Add(SubscriptionInvoice.CreateDraft(
            invoiceId,
            tenantId,
            subscriptionId,
            "INV-6BC-001",
            120m,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            Now.AddDays(7),
            "LKR",
            Now,
            Now.AddMonths(1),
            Now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
