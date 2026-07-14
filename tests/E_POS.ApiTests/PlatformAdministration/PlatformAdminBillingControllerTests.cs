using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAdminBillingControllerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Summary_Success_UsesExistingApiEnvelope()
    {
        var controller = Controller(new FakeService());
        var ok = Assert.IsType<OkObjectResult>(await controller.Summary(new(), default));
        var envelope = Assert.IsType<LegacyApiResponse<PlatformBillingSummaryResponse>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Equal(1, envelope.Data.TotalInvoices);
    }

    [Fact]
    public async Task InvoiceList_Success_ReturnsEnvelope()
    {
        var ok = Assert.IsType<OkObjectResult>(await Controller(new FakeService()).Invoices(new(), default));
        Assert.Single(Assert.IsType<LegacyApiResponse<PlatformBillingInvoiceListResponse>>(ok.Value).Data.Items);
    }

    [Fact]
    public async Task InvoiceDetail_Success_ReturnsEnvelope()
    {
        var ok = Assert.IsType<OkObjectResult>(await Controller(new FakeService()).Invoice(Guid.NewGuid(), default));
        Assert.Equal("INV-1", Assert.IsType<LegacyApiResponse<PlatformBillingInvoiceDetailResponse>>(ok.Value).Data.Invoice.InvoiceNumber);
    }

    [Fact]
    public async Task PaymentHistory_Success_ReturnsEnvelope()
    {
        var ok = Assert.IsType<OkObjectResult>(await Controller(new FakeService()).Payments(Guid.NewGuid(), default));
        Assert.Single(Assert.IsType<LegacyApiResponse<IReadOnlyList<PlatformBillingPaymentDto>>>(ok.Value).Data);
    }

    [Fact]
    public async Task MissingViewPermission_ReturnsForbidden()
    {
        var service = new FakeService { SummaryResult = Failure<PlatformBillingSummaryResponse>("access_denied") };
        var result = Assert.IsType<ObjectResult>(await Controller(service).Summary(new(), default));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task MissingManagePermission_ReturnsForbidden()
    {
        var service = new FakeService { MutationResult = Failure<PlatformBillingInvoiceDto>("access_denied") };
        var result = Assert.IsType<ObjectResult>(await Controller(service).Issue(Guid.NewGuid(), new(Now), default));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task MissingUserClaim_ReturnsUnauthorized()
    {
        Assert.IsType<UnauthorizedObjectResult>(await Controller(new FakeService(), authenticated: false).Summary(new(), default));
    }

    [Fact]
    public async Task InvoiceNotFound_Returns404()
    {
        var service = new FakeService { DetailResult = Failure<PlatformBillingInvoiceDetailResponse>("invoice_not_found") };
        Assert.IsType<NotFoundObjectResult>(await Controller(service).Invoice(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task InvalidRequest_Returns400()
    {
        var service = new FakeService { ListResult = Failure<PlatformBillingInvoiceListResponse>("validation_failed") };
        Assert.IsType<BadRequestObjectResult>(await Controller(service).Invoices(new(), default));
    }

    [Theory]
    [InlineData("invalid_transition")]
    [InlineData("concurrency_conflict")]
    public async Task InvalidTransitionAndConcurrency_Return409(string code)
    {
        var service = new FakeService { MutationResult = Failure<PlatformBillingInvoiceDto>(code) };
        Assert.IsType<ConflictObjectResult>(await Controller(service).Issue(Guid.NewGuid(), new(Now), default));
    }

    private static PlatformAdminBillingController Controller(IPlatformBillingService service, bool authenticated = true)
    {
        var context = new DefaultHttpContext();
        if (authenticated) context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", UserId.ToString()) }, "test"));
        return new(service) { ControllerContext = new ControllerContext { HttpContext = context } };
    }

    private static ApplicationResult<T> Failure<T>(string code) => ApplicationResult<T>.Failure(new ApplicationError($"platform_billing.{code}", code));

    private static PlatformBillingInvoiceDto Invoice() => new(Guid.NewGuid(), "INV-1", Guid.NewGuid(), "TEN", "Tenant", Guid.NewGuid(), "ACTIVE", Guid.NewGuid(), "PRO", "Pro", "LKR", 100m, 0m, 100m, "DRAFT", "DRAFT", null, Now.AddDays(10), null, Now, Now, true, false);

    private sealed class FakeService : IPlatformBillingService
    {
        private static readonly PlatformBillingInvoiceDto Item = Invoice();
        public ApplicationResult<PlatformBillingSummaryResponse> SummaryResult { get; set; } = ApplicationResult<PlatformBillingSummaryResponse>.Success(new PlatformBillingSummaryResponse([new("LKR", 0, 0, 0, 1)], 1, Now));
        public ApplicationResult<PlatformBillingInvoiceListResponse> ListResult { get; set; } = ApplicationResult<PlatformBillingInvoiceListResponse>.Success(new PlatformBillingInvoiceListResponse([Item], 1, 10, 1, 1));
        public ApplicationResult<PlatformBillingInvoiceDetailResponse> DetailResult { get; set; } = ApplicationResult<PlatformBillingInvoiceDetailResponse>.Success(new PlatformBillingInvoiceDetailResponse(Item, "SUBSCRIPTION", "MONTHLY", null, null, 100, 0, 0, [], []));
        public ApplicationResult<IReadOnlyList<PlatformBillingPaymentDto>> PaymentsResult { get; set; } = ApplicationResult<IReadOnlyList<PlatformBillingPaymentDto>>.Success([new(Guid.NewGuid(), "MANUAL", "PAY-1", "SUCCEEDED", "LKR", 100, 0, 100, Now, Now)]);
        public ApplicationResult<PlatformBillingInvoiceDto> MutationResult { get; set; } = ApplicationResult<PlatformBillingInvoiceDto>.Success(Item);
        public Task<ApplicationResult<PlatformBillingSummaryResponse>> GetSummaryAsync(PlatformBillingQuery q, Guid u, CancellationToken c) => Task.FromResult(SummaryResult);
        public Task<ApplicationResult<PlatformBillingInvoiceListResponse>> GetInvoicesAsync(PlatformBillingQuery q, Guid u, CancellationToken c) => Task.FromResult(ListResult);
        public Task<ApplicationResult<PlatformBillingInvoiceDetailResponse>> GetInvoiceAsync(Guid i, Guid u, CancellationToken c) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<IReadOnlyList<PlatformBillingPaymentDto>>> GetPaymentsAsync(Guid i, Guid u, CancellationToken c) => Task.FromResult(PaymentsResult);
        public Task<ApplicationResult<PlatformBillingFilterOptionsResponse>> GetFilterOptionsAsync(Guid u, CancellationToken c) => Task.FromResult(ApplicationResult<PlatformBillingFilterOptionsResponse>.Success(new([], [])));
        public Task<ApplicationResult<PlatformBillingInvoiceDto>> IssueAsync(Guid i, PlatformBillingTransitionRequest r, Guid u, CancellationToken c) => Task.FromResult(MutationResult);
        public Task<ApplicationResult<PlatformBillingInvoiceDto>> MarkPaidAsync(Guid i, PlatformBillingMarkPaidRequest r, Guid u, CancellationToken c) => Task.FromResult(MutationResult);
    }
}
