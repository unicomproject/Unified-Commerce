using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformBillingServiceTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Summary_ReturnsPaidOutstandingOverdueAndCurrencySeparatedTotals()
    {
        var repository = new FakeRepository
        {
            Summary = new(new[]
            {
                new PlatformBillingCurrencySummaryDto("LKR", 1000m, 300m, 100m, 3),
                new PlatformBillingCurrencySummaryDto("USD", 25m, 10m, 5m, 2)
            }, 5, Now)
        };
        var result = await Service(repository).GetSummaryAsync(Query(), UserId, default);
        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value!.Currencies,
            lkr => Assert.Equal(("LKR", 1000m, 300m, 100m), (lkr.CurrencyCode, lkr.PaidRevenue, lkr.OutstandingAmount, lkr.OverdueAmount)),
            usd => Assert.Equal(("USD", 25m, 10m, 5m), (usd.CurrencyCode, usd.PaidRevenue, usd.OutstandingAmount, usd.OverdueAmount)));
    }

    [Fact]
    public async Task Summary_InvalidDateRange_ReturnsValidationFailureWithoutRepositoryCall()
    {
        var repository = new FakeRepository();
        var result = await Service(repository).GetSummaryAsync(Query() with { DateFrom = Now, DateTo = Now.AddDays(-1) }, UserId, default);
        Assert.Equal("platform_billing.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.SummaryCalls);
    }

    [Theory]
    [InlineData("unsupported", "desc")]
    [InlineData("createdAt", "sideways")]
    public async Task Invoices_InvalidSorting_ReturnsValidationFailure(string sortBy, string direction)
    {
        var result = await Service(new FakeRepository()).GetInvoicesAsync(Query() with { SortBy = sortBy, SortDirection = direction }, UserId, default);
        Assert.Equal("platform_billing.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task Read_WithoutViewPermission_ReturnsAccessDenied()
    {
        var result = await Service(new FakeRepository(), view: false).GetInvoicesAsync(Query(), UserId, default);
        Assert.Equal("platform_billing.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task Issue_DraftToPending_ReturnsUpdatedInvoice()
    {
        var repository = new FakeRepository { Mutation = new(PlatformBillingMutationOutcome.Success, Invoice("PENDING")) };
        var result = await Service(repository).IssueAsync(Guid.NewGuid(), new(Now), UserId, default);
        Assert.Equal("PENDING", result.Value!.StoredStatus);
    }

    [Fact]
    public async Task MarkPaid_PendingToPaid_ReturnsSettledInvoice()
    {
        var repository = new FakeRepository { Mutation = new(PlatformBillingMutationOutcome.Success, Invoice("PAID") with { PaidAmount = 100m, BalanceDue = 0m }) };
        var result = await Service(repository).MarkPaidAsync(Guid.NewGuid(), new(Now), UserId, default);
        Assert.Equal("PAID", result.Value!.StoredStatus);
        Assert.Equal(0m, result.Value.BalanceDue);
    }

    [Theory]
    [InlineData(PlatformBillingMutationOutcome.InvalidTransition, "platform_billing.invalid_transition")]
    [InlineData(PlatformBillingMutationOutcome.ConcurrencyConflict, "platform_billing.concurrency_conflict")]
    public async Task MutationFailures_AreMapped(PlatformBillingMutationOutcome outcome, string code)
    {
        var repository = new FakeRepository { Mutation = new(outcome) };
        var result = await Service(repository).IssueAsync(Guid.NewGuid(), new(Now), UserId, default);
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public async Task AlreadyPaidInvoice_IsReturnedAsInvalidTransition()
    {
        var repository = new FakeRepository { Mutation = new(PlatformBillingMutationOutcome.InvalidTransition) };
        var result = await Service(repository).MarkPaidAsync(Guid.NewGuid(), new(Now), UserId, default);
        Assert.Equal("platform_billing.invalid_transition", result.Error.Code);
    }

    [Fact]
    public async Task Mutation_WithoutManagePermission_ReturnsAccessDeniedWithoutRepositoryCall()
    {
        var repository = new FakeRepository();
        var result = await Service(repository, manage: false).IssueAsync(Guid.NewGuid(), new(Now), UserId, default);
        Assert.Equal("platform_billing.access_denied", result.Error.Code);
        Assert.Equal(0, repository.MutationCalls);
    }

    private static PlatformBillingService Service(FakeRepository repository, bool view = true, bool manage = true) =>
        new(repository, new FakePermissionChecker(view, manage), new FakeClock());

    private static PlatformBillingQuery Query() => new(SortBy: "createdAt", SortDirection: "desc");

    private static PlatformBillingInvoiceDto Invoice(string status) => new(
        Guid.NewGuid(), "INV-1", Guid.NewGuid(), "TEN", "Tenant", Guid.NewGuid(), "ACTIVE", Guid.NewGuid(), "PRO", "Pro",
        "LKR", 100m, status == "PAID" ? 100m : 0m, status == "PAID" ? 0m : 100m, status, status, Now, Now.AddDays(14),
        status == "PAID" ? Now : null, Now, Now, status == "DRAFT", status == "PENDING");

    private sealed class FakeRepository : IPlatformBillingRepository
    {
        public PlatformBillingSummaryResponse Summary { get; set; } = new([], 0, Now);
        public PlatformBillingMutationResult Mutation { get; set; } = new(PlatformBillingMutationOutcome.Success, Invoice("PENDING"));
        public int SummaryCalls { get; private set; }
        public int MutationCalls { get; private set; }
        public Task<PlatformBillingSummaryResponse> GetSummaryAsync(PlatformBillingQuery query, DateTimeOffset now, CancellationToken ct) { SummaryCalls++; return Task.FromResult(Summary); }
        public Task<PlatformBillingInvoiceListResponse> GetInvoicesAsync(PlatformBillingQuery query, DateTimeOffset now, CancellationToken ct) => Task.FromResult(new PlatformBillingInvoiceListResponse([], 1, 10, 0, 0));
        public Task<PlatformBillingInvoiceDetailResponse?> GetInvoiceAsync(Guid id, DateTimeOffset now, CancellationToken ct) => Task.FromResult<PlatformBillingInvoiceDetailResponse?>(null);
        public Task<IReadOnlyList<PlatformBillingPaymentDto>?> GetPaymentsAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<PlatformBillingPaymentDto>?>([]);
        public Task<PlatformBillingFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken ct) => Task.FromResult(new PlatformBillingFilterOptionsResponse([], []));
        public Task<PlatformBillingMutationResult> IssueAsync(Guid id, DateTimeOffset expected, DateTimeOffset now, CancellationToken ct) { MutationCalls++; return Task.FromResult(Mutation); }
        public Task<PlatformBillingMutationResult> MarkPaidAsync(Guid id, DateTimeOffset expected, DateTimeOffset paidAt, DateTimeOffset now, CancellationToken ct) { MutationCalls++; return Task.FromResult(Mutation); }
    }

    private sealed class FakePermissionChecker(bool view, bool manage) : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, string code, CancellationToken ct) => Task.FromResult(code switch
        {
            PlatformPermissionCodes.BillingView => view,
            PlatformPermissionCodes.BillingManage => manage,
            _ => false
        });
    }

    private sealed class FakeClock : IDateTimeProvider { public DateTimeOffset UtcNow => Now; }
}
