using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class SubscriptionBillingAlignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 4, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateDraftInvoice_DualWritesLegacyAndSecondBrainColumns()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-BILL",
            "ten-bill",
            "Billing Tenant",
            "pending",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "BILLING",
            "Billing Plan",
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

        var invoice = SubscriptionInvoice.CreateDraft(
            Guid.NewGuid(),
            tenantId,
            subscriptionId,
            "INV-TEST-001",
            120m,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            Now.AddDays(7),
            "LKR",
            Now,
            Now.AddMonths(1),
            Now);

        dbContext.SubscriptionInvoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.SubscriptionInvoices.SingleAsync(x => x.Id == invoice.Id);

        Assert.Equal(subscriptionId, saved.TenantSubscriptionId);
        Assert.Equal(subscriptionId, saved.SubscriptionId);
        Assert.Equal(SubscriptionBillingAlignmentConstants.InvoiceTypeSubscription, saved.InvoiceType);
        Assert.Equal("LKR", saved.CurrencyCode);
        Assert.Equal(120m, saved.TotalAmount);
        Assert.Equal(120m, saved.SubtotalAmount);
        Assert.Equal(0m, saved.DiscountAmount);
        Assert.Equal(0m, saved.TaxAmount);
        Assert.Equal(0m, saved.PaidAmount);
        Assert.Equal(120m, saved.BalanceDue);
        Assert.Equal(TenantSubscriptionBillingConstants.InvoiceStatusDraft, saved.InvoiceStatus);
        Assert.Equal(Now, saved.BillingPeriodStart);
        Assert.Equal(Now.AddMonths(1), saved.BillingPeriodEnd);
    }

    [Fact]
    public async Task InvoiceLine_PersistsSecondBrainAliasAndMoneyColumns()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var lineId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-LINE",
            "ten-line",
            "Line Tenant",
            "pending",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "LINE-PLAN",
            "Line Plan",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            50m,
            Now));

        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            subscriptionId,
            tenantId,
            planId,
            TenantSubscriptionStatusConstants.Active,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            trialStartAt: null,
            trialEndAt: null,
            billingStartAt: Now,
            nextBillingAt: null,
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
            planPrice: 50m,
            startedAt: Now,
            currentPeriodStart: Now,
            currentPeriodEnd: null,
            assignedByPlatformUserId: null,
            createdAt: Now));

        dbContext.SubscriptionInvoices.Add(SubscriptionInvoice.CreateDraft(
            invoiceId,
            tenantId,
            subscriptionId,
            "INV-LINE-001",
            50m,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            Now.AddDays(7),
            "LKR",
            Now,
            null,
            Now));

        dbContext.SubscriptionInvoiceLines.Add(SubscriptionInvoiceLine.Create(
            lineId,
            invoiceId,
            "1",
            1,
            SubscriptionBillingAlignmentConstants.InvoiceLineTypePlan,
            "Starter plan",
            1m,
            50m,
            50m,
            Now));

        await dbContext.SaveChangesAsync();

        var saved = await dbContext.SubscriptionInvoiceLines.SingleAsync(x => x.Id == lineId);

        Assert.Equal(invoiceId, saved.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, saved.InvoiceId);
        Assert.Equal(1, saved.LineNumberInt);
        Assert.Equal(50m, saved.LineTotal);
        Assert.Equal(50m, saved.LineTotalAmount);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
