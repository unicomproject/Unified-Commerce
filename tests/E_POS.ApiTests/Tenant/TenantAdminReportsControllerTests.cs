using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.Reports;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.Tenant;

/// <summary>
/// The Reporting HTTP surface must let the client tell REP-S02 (empty success), REP-S03 (error)
/// and REP-S06 (access denied) apart, and must only accept tenant identities.
/// </summary>
public sealed class TenantAdminReportsControllerTests
{
    [Fact]
    public void Controller_RequiresTenantOnlyPolicy_SoPlatformAdminTokensCannotReadTenantReports()
    {
        var authorize = Assert.Single(typeof(TenantAdminReportsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
        Assert.DoesNotContain(typeof(TenantAdminReportsController).GetMethods(), m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null);
    }

    [Fact]
    public async Task EmptyReport_Returns200WithEmptyRows()
    {
        var empty = new ReportResultDto("payments", "LKR", "Asia/Colombo", null, null,
            new Dictionary<string, object?> { ["successfulReceipts"] = 0m }, new Dictionary<string, object?>(), [], null, DateTimeOffset.UtcNow);
        var controller = CreateController(new FakeReportsService(ApplicationResult<ReportResultDto>.Success(empty)));

        var result = await controller.GetSales(section: "payments");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode ?? StatusCodes.Status200OK);
    }

    [Fact]
    public async Task PermissionDenied_Returns403_AndNotFound_Returns404()
    {
        var denied = CreateController(new FakeReportsService(ApplicationResult<ReportResultDto>.Failure(new("reports.permission_denied", "denied"))));
        var missing = CreateController(new FakeReportsService(ApplicationResult<ReportResultDto>.Failure(new("reports.not_found", "missing"))));

        var forbidden = Assert.IsType<ObjectResult>(await denied.GetSales(outletId: Guid.NewGuid()));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.IsType<NotFoundObjectResult>(await missing.GetSalesDetail(Guid.NewGuid()));
    }

    [Fact]
    public async Task BackendFailure_IsNotConvertedIntoAnEmptyReport()
    {
        var controller = CreateController(new FakeReportsService(null, new TimeoutException("database unavailable")));

        // Propagates to GlobalExceptionHandlingMiddleware (5xx), never a 200 with zero totals.
        await Assert.ThrowsAsync<TimeoutException>(() => controller.GetSales(section: "transactions"));
    }

    [Fact]
    public async Task MissingTenantClaims_Returns401()
    {
        var controller = CreateController(new FakeReportsService(null), withClaims: false);

        Assert.IsType<UnauthorizedObjectResult>(await controller.GetSales());
    }

    private static TenantAdminReportsController CreateController(FakeReportsService service, bool withClaims = true)
    {
        var controller = new TenantAdminReportsController(service, new FakeTenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (withClaims)
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("tenant_id", Guid.NewGuid().ToString()),
                new Claim("permissions", "tenant.reports.sales.view")
            ], "test"));
        return controller;
    }

    private sealed class FakeTenantRequestContextFactory : ITenantRequestContextFactory
    {
        public bool TryCreate(ClaimsPrincipal user, out TenantRequestContext context)
        {
            if (Guid.TryParse(user.FindFirstValue("sub"), out var userId) && Guid.TryParse(user.FindFirstValue("tenant_id"), out var tenantId))
            {
                context = new TenantRequestContext(tenantId, userId, user.FindAll("permissions").Select(x => x.Value).ToArray());
                return true;
            }
            context = new TenantRequestContext(Guid.Empty, Guid.Empty, []);
            return false;
        }
    }

    private sealed class FakeReportsService(ApplicationResult<ReportResultDto>? report, Exception? failure = null) : ITenantAdminReportsService
    {
        private Task<ApplicationResult<T>> Respond<T>(ApplicationResult<T>? value)
        {
            if (failure is not null) throw failure;
            return Task.FromResult(value ?? ApplicationResult<T>.Failure(new("reports.not_found", "missing")));
        }

        private ApplicationResult<T>? Map<T>() => report is { IsSuccess: false } ? ApplicationResult<T>.Failure(report.Error) : null;

        public Task<ApplicationResult<ReportFilterOptionsResponse>> GetFilterOptionsAsync(TenantRequestContext context, ReportFilterOptionsRequest request, CancellationToken cancellationToken) => Respond(Map<ReportFilterOptionsResponse>());
        public Task<ApplicationResult<ReportResultDto>> GetDashboardAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) => Respond(report);
        public Task<ApplicationResult<ReportResultDto>> GetSalesAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) => Respond(report);
        public Task<ApplicationResult<ReportResultDto>> GetStockAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) => Respond(report);
        public Task<ApplicationResult<ReportResultDto>> GetOutletsAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) => Respond(report);
        public Task<ApplicationResult<SalesTransactionDetailDto>> GetSalesTransactionDetailAsync(TenantRequestContext context, Guid orderId, CancellationToken cancellationToken) => Respond(Map<SalesTransactionDetailDto>());
        public Task<ApplicationResult<ReportExportDto>> CreateExportAsync(TenantRequestContext context, ReportExportRequest request, CancellationToken cancellationToken) => Respond(Map<ReportExportDto>());
        public Task<ApplicationResult<ReportExportDto>> GetExportAsync(TenantRequestContext context, Guid jobId, CancellationToken cancellationToken) => Respond(Map<ReportExportDto>());
        public Task<ApplicationResult<byte[]>> DownloadExportAsync(TenantRequestContext context, Guid jobId, CancellationToken cancellationToken) => Respond(Map<byte[]>());
    }
}
