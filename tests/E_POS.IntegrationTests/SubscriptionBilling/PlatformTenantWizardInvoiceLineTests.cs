using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class PlatformTenantWizardInvoiceLineTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTenantWizardAsync_WithDraftInvoice_PersistsInvoiceLinesInSameTransaction()
    {
        await using var dbContext = CreateDbContext();
        var repository = new PlatformTenantRepository(dbContext);

        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        const decimal planPrice = 99.50m;
        const decimal addonUnitPrice = 12.25m;
        const int addonQuantity = 2;
        var addonLineTotal = addonUnitPrice * addonQuantity;
        var invoiceTotal = planPrice + addonLineTotal;

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "WIZARD_PLAN",
            "Wizard Plan",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            planPrice,
            Now,
            baseCurrency: "LKR"));

        var tenant = Tenant.Create(
            tenantId,
            "TEN-WIZ-INV",
            "ten-wiz-inv",
            "Wizard Invoice Tenant",
            TenantBillingStatusConstants.Pending,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now);

        var subscription = TenantSubscription.Create(
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
            planPrice: planPrice,
            startedAt: Now,
            currentPeriodStart: Now,
            currentPeriodEnd: Now.AddMonths(1),
            assignedByPlatformUserId: null,
            createdAt: Now);

        var draftInvoice = SubscriptionInvoice.CreateDraft(
            invoiceId,
            tenantId,
            subscriptionId,
            "INV-WIZ-001",
            invoiceTotal,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            Now.AddDays(7),
            "LKR",
            Now,
            Now.AddMonths(1),
            Now);

        var draftInvoiceLines = new List<SubscriptionInvoiceLine>
        {
            SubscriptionInvoiceLine.Create(
                Guid.NewGuid(),
                invoiceId,
                "1",
                1,
                SubscriptionBillingAlignmentConstants.InvoiceLineTypePlan,
                "Wizard Plan",
                1m,
                planPrice,
                planPrice,
                Now),
            SubscriptionInvoiceLine.Create(
                Guid.NewGuid(),
                invoiceId,
                "2",
                2,
                SubscriptionBillingAlignmentConstants.InvoiceLineTypeAddon,
                "Extra Outlet",
                addonQuantity,
                addonUnitPrice,
                addonLineTotal,
                Now)
        };

        await repository.CreateTenantWizardAsync(
            new PlatformTenantCreateWriteModel
            {
                Tenant = tenant,
                Subscription = subscription,
                DraftInvoice = draftInvoice,
                DraftInvoiceLines = draftInvoiceLines
            },
            CancellationToken.None);

        var savedInvoice = await dbContext.SubscriptionInvoices.SingleAsync(x => x.Id == invoiceId);
        var savedLines = await dbContext.SubscriptionInvoiceLines
            .Where(x => x.SubscriptionInvoiceId == invoiceId)
            .OrderBy(x => x.LineNumberInt)
            .ToListAsync();

        Assert.Equal(invoiceTotal, savedInvoice.TotalAmount);
        Assert.Equal(2, savedLines.Count);

        var planLine = savedLines[0];
        Assert.Equal(SubscriptionBillingAlignmentConstants.InvoiceLineTypePlan, planLine.ItemType);
        Assert.Equal(invoiceId, planLine.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, planLine.InvoiceId);
        Assert.Equal(1, planLine.LineNumberInt);
        Assert.Equal("1", planLine.LineNumber);
        Assert.Equal(planPrice, planLine.LineTotalAmount);
        Assert.Equal(planPrice, planLine.LineTotal);

        var addonLine = savedLines[1];
        Assert.Equal(SubscriptionBillingAlignmentConstants.InvoiceLineTypeAddon, addonLine.ItemType);
        Assert.Equal(invoiceId, addonLine.SubscriptionInvoiceId);
        Assert.Equal(invoiceId, addonLine.InvoiceId);
        Assert.Equal(2, addonLine.LineNumberInt);
        Assert.Equal("2", addonLine.LineNumber);
        Assert.Equal(addonLineTotal, addonLine.LineTotalAmount);
        Assert.Equal(addonLineTotal, addonLine.LineTotal);

        Assert.Equal(savedInvoice.TotalAmount, savedLines.Sum(line => line.LineTotalAmount));
        Assert.Equal(savedInvoice.SubtotalAmount, savedLines.Sum(line => line.LineTotal));
        Assert.Equal(savedInvoice.BalanceDue, savedLines.Sum(line => line.LineTotalAmount));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new EPosDbContext(options);
    }
}
