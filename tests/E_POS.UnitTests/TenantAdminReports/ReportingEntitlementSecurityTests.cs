using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using System.Runtime.Serialization;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingEntitlementSecurityTests
    {
        private T Dummy<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

        private (TenantAdminReportsService Service, Mock<ITenantFeatureEntitlementEvaluator> Entitlements, Mock<ITenantAdminReportsRepository> Repo) CreateService()
        {
            var repo = new Mock<ITenantAdminReportsRepository>();
            var entitlements = new Mock<ITenantFeatureEntitlementEvaluator>();
            var clock = new Mock<IDateTimeProvider>();
            clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
            var service = new TenantAdminReportsService(repo.Object, entitlements.Object, clock.Object);
            return (service, entitlements, repo);
        }

        private TenantRequestContext CreateContext(bool hasPermission) =>
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), hasPermission ? new List<string> { "tenant.reports.sales.view" } : new List<string>());

        private TenantRequestContext CreateStockContext(bool hasPermission) =>
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), hasPermission ? new List<string> { "tenant.stock.view" } : new List<string>());

        private TenantRequestContext CreateExportContext(bool hasPermission) =>
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), hasPermission ? new List<string> { "tenant.reports.export", "tenant.reports.sales.view" } : new List<string>());

        [Fact] public async Task Sales_PermissionYes_EntitlementYes_Allowed() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), TenantAdminReportFeatureCodes.SalesReports, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); repo.Setup(r => r.GetSalesAsync(It.IsAny<TenantRequestContext>(), It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(ReportResultDto)!); var result = await svc.GetSalesAsync(CreateContext(true), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.True(result.IsSuccess); }
        [Fact] public async Task Sales_PermissionYes_EntitlementNo_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.GetSalesAsync(CreateContext(true), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Sales_PermissionNo_EntitlementYes_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); var result = await svc.GetSalesAsync(CreateContext(false), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Sales_PermissionNo_EntitlementNo_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.GetSalesAsync(CreateContext(false), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Stock_PermissionYes_EntitlementYes_Allowed() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), TenantAdminReportFeatureCodes.InventoryReports, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); repo.Setup(r => r.GetStockAsync(It.IsAny<TenantRequestContext>(), It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(ReportResultDto)!); var result = await svc.GetStockAsync(CreateStockContext(true), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.True(result.IsSuccess); }
        [Fact] public async Task Stock_PermissionYes_EntitlementNo_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.GetStockAsync(CreateStockContext(true), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Stock_PermissionNo_EntitlementYes_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); var result = await svc.GetStockAsync(CreateStockContext(false), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Stock_PermissionNo_EntitlementNo_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.GetStockAsync(CreateStockContext(false), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Export_PermissionYes_EntitlementYes_Allowed() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), TenantAdminReportFeatureCodes.ReportExport, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); var result = await svc.CreateExportAsync(CreateExportContext(true), new ReportExportRequest("SALES", "SALES", "CSV", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null)), CancellationToken.None); Assert.NotEqual("reports.permission_denied", result.Error?.Code); }
        [Fact] public async Task Export_PermissionYes_EntitlementNo_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.CreateExportAsync(CreateExportContext(true), new ReportExportRequest("SALES", "SALES", "CSV", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null)), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Export_PermissionNo_EntitlementYes_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); var result = await svc.CreateExportAsync(CreateExportContext(false), new ReportExportRequest("SALES", "SALES", "CSV", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null)), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Export_PermissionNo_EntitlementNo_Denied() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.CreateExportAsync(CreateExportContext(false), new ReportExportRequest("SALES", "SALES", "CSV", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null)), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task Dashboard_NoEntitlement_FailsClosed() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.GetDashboardAsync(CreateContext(true), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "summary", 1, 25, null, null, null, null, null, null, null), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task FilterOptions_NoEntitlement_FailsClosed() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); var result = await svc.GetFilterOptionsAsync(CreateContext(true), new ReportFilterOptionsRequest(null, null, null, null, null, "options", false, 1, 25), CancellationToken.None); Assert.False(result.IsSuccess); Assert.Equal("reports.permission_denied", result.Error.Code); }
        [Fact] public async Task FilterOptions_YesEntitlement_YesPermission_Allowed() { var (svc, ent, repo) = CreateService(); ent.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); repo.Setup(r => r.GetFilterOptionsAsync(It.IsAny<TenantRequestContext>(), It.IsAny<ReportFilterOptionsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(ReportFilterOptionsResponse)!); var result = await svc.GetFilterOptionsAsync(CreateContext(true), new ReportFilterOptionsRequest(null, null, null, null, null, "options", false, 1, 25), CancellationToken.None); Assert.True(result.IsSuccess); }
    }
}





