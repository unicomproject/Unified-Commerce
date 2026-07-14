using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformBillingRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task List_UsesServerPaginationAndStableDefaultOrdering()
    {
        await using var data = await SeedAsync();
        var first = await data.Repository.GetInvoicesAsync(Query(page: 1, size: 2), Now, default);
        var second = await data.Repository.GetInvoicesAsync(Query(page: 2, size: 2), Now, default);
        Assert.Equal(4, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(new[] { "INV-004", "INV-003" }, first.Items.Select(x => x.InvoiceNumber));
        Assert.Equal(new[] { "INV-002", "INV-001" }, second.Items.Select(x => x.InvoiceNumber));
    }

    [Theory]
    [InlineData("INV-002", "INV-002")]
    [InlineData("alpha stores", "INV-001,INV-002,INV-004")]
    [InlineData("BETA", "INV-003")]
    public async Task List_SearchesInvoiceTenantNameAndCode(string search, string expected)
    {
        await using var data = await SeedAsync();
        var result = await data.Repository.GetInvoicesAsync(Query() with { Search = search }, Now, default);
        Assert.Equal(expected, string.Join(',', result.Items.OrderBy(x => x.InvoiceNumber).Select(x => x.InvoiceNumber)));
    }

    [Fact]
    public async Task List_FiltersByTenant()
    {
        await using var data = await SeedAsync();
        var result = await data.Repository.GetInvoicesAsync(Query() with { TenantId = data.BetaTenantId }, Now, default);
        Assert.Single(result.Items);
        Assert.Equal("INV-003", result.Items[0].InvoiceNumber);
    }

    [Theory]
    [InlineData("PAID", "INV-001")]
    [InlineData("PENDING", "INV-003")]
    [InlineData("OVERDUE", "INV-002")]
    [InlineData("DRAFT", "INV-004")]
    public async Task List_FiltersStoredAndDerivedStatuses(string status, string expected)
    {
        await using var data = await SeedAsync();
        var result = await data.Repository.GetInvoicesAsync(Query() with { Status = status }, Now, default);
        Assert.Single(result.Items);
        Assert.Equal(expected, result.Items[0].InvoiceNumber);
    }

    [Fact]
    public async Task List_FiltersDateFromAndTo()
    {
        await using var data = await SeedAsync();
        var result = await data.Repository.GetInvoicesAsync(Query() with
        {
            DateField = "issuedAt", DateFrom = Now.AddDays(-3), DateTo = Now.AddDays(-1)
        }, Now, default);
        Assert.Equal(new[] { "INV-003", "INV-002" }, result.Items.Select(x => x.InvoiceNumber));
    }

    [Fact]
    public async Task List_AppliesCombinedFilters()
    {
        await using var data = await SeedAsync();
        var result = await data.Repository.GetInvoicesAsync(Query() with
        {
            TenantId = data.AlphaTenantId, Status = "OVERDUE", Search = "alpha", DateField = "dueAt",
            DateFrom = Now.AddDays(-2), DateTo = Now
        }, Now, default);
        Assert.Single(result.Items);
        Assert.Equal("INV-002", result.Items[0].InvoiceNumber);
    }

    [Fact]
    public async Task Detail_ReturnsInvoiceAndLines()
    {
        await using var data = await SeedAsync();
        var detail = await data.Repository.GetInvoiceAsync(data.PaidInvoiceId, Now, default);
        Assert.NotNull(detail);
        Assert.Equal("INV-001", detail.Invoice.InvoiceNumber);
        Assert.Single(detail.Lines);
        Assert.Equal("Subscription charge", detail.Lines[0].Description);
    }

    [Fact]
    public async Task Payments_ReturnsHistoryAndMissingInvoiceReturnsNull()
    {
        await using var data = await SeedAsync();
        var payments = await data.Repository.GetPaymentsAsync(data.PaidInvoiceId, default);
        Assert.Single(payments!);
        Assert.Equal("SUCCEEDED", payments![0].Status);
        Assert.Null(await data.Repository.GetPaymentsAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Summary_GroupsCurrenciesAndUsesPaidPendingAndDerivedOverdueSemantics()
    {
        await using var data = await SeedAsync();
        var summary = await data.Repository.GetSummaryAsync(Query(), Now, default);
        Assert.Equal(4, summary.TotalInvoices);
        Assert.Collection(summary.Currencies,
            lkr => Assert.Equal(("LKR", 100m, 50m, 50m, 3), (lkr.CurrencyCode, lkr.PaidRevenue, lkr.OutstandingAmount, lkr.OverdueAmount, lkr.InvoiceCount)),
            usd => Assert.Equal(("USD", 0m, 20m, 0m, 1), (usd.CurrencyCode, usd.PaidRevenue, usd.OutstandingAmount, usd.OverdueAmount, usd.InvoiceCount)));
    }

    [Fact]
    public async Task EmptyFilters_ReturnEmptyPageAndSummary()
    {
        await using var data = await SeedAsync();
        var filter = Query() with { Search = "does-not-exist" };
        var list = await data.Repository.GetInvoicesAsync(filter, Now, default);
        var summary = await data.Repository.GetSummaryAsync(filter, Now, default);
        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalCount);
        Assert.Empty(summary.Currencies);
    }

    private static PlatformBillingQuery Query(int page = 1, int size = 20) => new(PageNumber: page, PageSize: size);

    private static async Task<SeededData> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new EPosDbContext(options);
        var alphaId = Guid.NewGuid(); var betaId = Guid.NewGuid();
        var alphaPlanId = Guid.NewGuid(); var betaPlanId = Guid.NewGuid();
        var alphaSubscriptionId = Guid.NewGuid(); var betaSubscriptionId = Guid.NewGuid();
        db.Tenants.AddRange(
            Tenant.Create(alphaId, "ALPHA", "alpha", "Alpha Stores", "active", "LKR", "UTC", null, null, Now.AddMonths(-2)),
            Tenant.Create(betaId, "BETA", "beta", "Beta Labs", "active", "USD", "UTC", null, null, Now.AddMonths(-1)));
        db.SubscriptionPlans.AddRange(
            SubscriptionPlan.Create(alphaPlanId, "PRO", "Professional", "active", "MONTHLY", 100m, Now.AddMonths(-3), "LKR"),
            SubscriptionPlan.Create(betaPlanId, "USD-PRO", "USD Professional", "active", "MONTHLY", 20m, Now.AddMonths(-3), "USD"));
        db.TenantSubscriptions.AddRange(
            TenantSubscription.Create(alphaSubscriptionId, alphaId, alphaPlanId, "ACTIVE", Now.AddMonths(-2)),
            TenantSubscription.Create(betaSubscriptionId, betaId, betaPlanId, "ACTIVE", Now.AddMonths(-1)));

        var paid = Draft("INV-001", alphaId, alphaSubscriptionId, 100m, "LKR", Now.AddDays(-4), Now.AddDays(-1));
        paid.Issue(Now.AddDays(-4)); paid.MarkPaid(Now.AddDays(-3), Now.AddDays(-3));
        var overdue = Draft("INV-002", alphaId, alphaSubscriptionId, 50m, "LKR", Now.AddDays(-3), Now.AddDays(-1));
        overdue.Issue(Now.AddDays(-3));
        var pendingUsd = Draft("INV-003", betaId, betaSubscriptionId, 20m, "USD", Now.AddDays(-2), Now.AddDays(5));
        pendingUsd.Issue(Now.AddDays(-2));
        var draft = Draft("INV-004", alphaId, alphaSubscriptionId, 30m, "LKR", Now.AddDays(-1), Now.AddDays(10));
        db.SubscriptionInvoices.AddRange(paid, overdue, pendingUsd, draft);
        db.SubscriptionInvoiceLines.Add(SubscriptionInvoiceLine.Create(Guid.NewGuid(), paid.Id, "1", 1, "SUBSCRIPTION", "Subscription charge", 1m, 100m, 100m, Now.AddDays(-4)));
        var payment = SubscriptionPaymentTransaction.CreatePending(Guid.NewGuid(), alphaId, paid.Id, Guid.NewGuid(), 100m, "LKR", "MANUAL", "PAY-001", Now.AddDays(-3));
        payment.MarkSucceeded(Now.AddDays(-3));
        db.SubscriptionPaymentTransactions.Add(payment);
        await db.SaveChangesAsync();
        return new(db, new PlatformBillingRepository(db), alphaId, betaId, paid.Id);
    }

    private static SubscriptionInvoice Draft(string number, Guid tenantId, Guid subscriptionId, decimal amount, string currency, DateTimeOffset created, DateTimeOffset due) =>
        SubscriptionInvoice.CreateDraft(Guid.NewGuid(), tenantId, subscriptionId, number, amount, "MONTHLY", due, currency, created, created.AddMonths(1), created);

    private sealed record SeededData(EPosDbContext Db, PlatformBillingRepository Repository, Guid AlphaTenantId, Guid BetaTenantId, Guid PaidInvoiceId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
