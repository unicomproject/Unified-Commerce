using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Application.Modules.Tenant.Reports.Validators;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using Xunit;

namespace E_POS.UnitTests.Reports;

public sealed class ReportFoundationTests
{
    [Fact]
    public void FromInstant_UsesTenantTimezoneBusinessDate()
    {
        var completedAt = new DateTimeOffset(2026, 07, 15, 20, 30, 00, TimeSpan.Zero);

        var businessDate = ReportBusinessDateCalculator.FromInstant(completedAt, "Asia/Colombo");

        Assert.Equal(new DateOnly(2026, 07, 16), businessDate);
    }

    [Fact]
    public void FromInstant_WithInvalidTimezone_FallsBackToUtc()
    {
        var completedAt = new DateTimeOffset(2026, 07, 15, 20, 30, 00, TimeSpan.Zero);

        var businessDate = ReportBusinessDateCalculator.FromInstant(completedAt, "Not/A-Timezone");

        Assert.Equal(new DateOnly(2026, 07, 15), businessDate);
    }

    [Fact]
    public void Resolve_ForPosSale_UsesTillOutletBeforeAssignedOutlet()
    {
        var tillOutletId = Guid.NewGuid();
        var assignedOutletId = Guid.NewGuid();

        var resolution = ReportingOutletResolver.Resolve(new ReportingOutletResolutionInput(
            "POS_SALE",
            TillOutletId: tillOutletId,
            TillOutletCode: " OUT-001 ",
            TillOutletName: " Main Outlet ",
            AssignedOutletId: assignedOutletId,
            AssignedOutletCode: "OUT-002",
            AssignedOutletName: "Other Outlet"));

        Assert.Equal(tillOutletId, resolution.OutletId);
        Assert.Equal("OUT-001", resolution.OutletCodeSnapshot);
        Assert.Equal("Main Outlet", resolution.OutletNameSnapshot);
        Assert.Equal("pos_till_outlet", resolution.Reason);
    }

    [Fact]
    public void SecurityPolicy_MasksCustomerPii_WithoutPermission()
    {
        Assert.Null(ReportSecurityPolicy.MaskEmail("customer@example.com", []));
        Assert.Null(ReportSecurityPolicy.MaskPhone("+94771234567", []));
    }

    [Fact]
    public void SecurityPolicy_ReturnsCustomerPii_WithPermission()
    {
        var permissions = new[] { TenantAdminReportPermissions.CustomerPiiView };

        Assert.Equal("customer@example.com", ReportSecurityPolicy.MaskEmail("customer@example.com", permissions));
        Assert.Equal("+94771234567", ReportSecurityPolicy.MaskPhone("+94771234567", permissions));
    }

    [Fact]
    public void SecurityPolicy_ProtectsStockValue_WithoutPermission()
    {
        Assert.Null(ReportSecurityPolicy.ProtectStockValue(123.45m, []));
        Assert.Equal(123.45m, ReportSecurityPolicy.ProtectStockValue(123.45m, [TenantAdminStockPermissions.ValueView]));
    }

    [Fact]
    public void Validator_RejectsInvalidPagingDateRangeAndSort()
    {
        var validator = new ReportQueryValidator(
            ["summary"],
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["summary"] = ["businessDate"]
            },
            31);

        var errors = validator.Validate(new ReportQueryDto(
            From: new DateOnly(2026, 01, 01),
            To: new DateOnly(2026, 02, 15),
            OutletId: null,
            TillId: null,
            CashierId: null,
            CustomerId: null,
            DepartmentId: null,
            CategoryId: null,
            SubcategoryId: null,
            BrandId: null,
            ProductId: null,
            ProductVariantId: null,
            SalesChannelId: null,
            PaymentMethodId: null,
            OrderStatus: null,
            PaymentStatus: null,
            StockStatus: null,
            ExpiryStatus: null,
            MovementType: null,
            BatchNumber: null,
            Search: null,
            Section: "unknown",
            Page: 0,
            PageSize: 10,
            SortBy: "unsupported",
            SortDirection: "sideways"));

        Assert.Contains("Page must be greater than or equal to 1.", errors);
        Assert.Contains("PageSize must be one of 25, 50, or 100.", errors);
        Assert.Contains("Report date range cannot exceed 31 days.", errors);
        Assert.Contains("Section is not supported.", errors);
        Assert.Contains("SortBy is not supported for this section.", errors);
        Assert.Contains("SortDirection must be asc or desc.", errors);
    }

    [Fact]
    public async Task CreateExportAsync_WithoutExportPermission_ReturnsPermissionDenied()
    {
        var service = new TenantAdminReportsService(new FakeReportsRepository());
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [TenantAdminReportPermissions.SalesView]);

        var result = await service.CreateExportAsync(context, CreateExportRequest("sales", "transactions", "csv"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("reports.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateExportAsync_WithPermissions_CreatesSafeCompletedJobMetadata()
    {
        var service = new TenantAdminReportsService(new FakeReportsRepository());
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView]);

        var result = await service.CreateExportAsync(context, CreateExportRequest("sales", "transactions", "csv"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("COMPLETED", result.Value!.Status);
        Assert.Equal("CSV", result.Value.Format);
        Assert.EndsWith(".csv", result.Value.FileName);
        Assert.DoesNotContain("..", result.Value.FileName);
        Assert.Null(result.Value.DownloadUrl);
    }

    private static ReportExportRequest CreateExportRequest(string reportType, string section, string format) =>
        new(reportType, section, format, new ReportQueryRequest(
            From: null,
            To: null,
            OutletId: null,
            TillId: null,
            CashierId: null,
            CustomerId: null,
            DepartmentId: null,
            CategoryId: null,
            SubcategoryId: null,
            BrandId: null,
            ProductId: null,
            ProductVariantId: null,
            SalesChannelId: null,
            PaymentMethodId: null,
            OrderStatus: null,
            PaymentStatus: null,
            Search: null,
            Section: section));

    private sealed class FakeReportsRepository : ITenantAdminReportsRepository
    {
        public Task<ReportFilterOptionsResponse> GetFilterOptionsAsync(TenantRequestContext context, ReportFilterOptionsRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReportResultDto> GetDashboardAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReportResultDto> GetSalesAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReportResultDto> GetStockAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReportResultDto> GetOutletsAsync(TenantRequestContext context, ReportQueryRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<SalesTransactionDetailDto?> GetSalesTransactionDetailAsync(TenantRequestContext context, Guid orderId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
