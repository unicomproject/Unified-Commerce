using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using Xunit;
using Moq;
using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingExportTests
    {
        private readonly Mock<ITenantAdminReportsRepository> _repository;
        private readonly Mock<ITenantFeatureEntitlementEvaluator> _entitlements;
        private readonly Mock<IDateTimeProvider> _clock;
        private readonly TenantAdminReportsService _service;
        
        public ReportingExportTests()
        {
            _repository = new Mock<ITenantAdminReportsRepository>();
            _entitlements = new Mock<ITenantFeatureEntitlementEvaluator>();
            _clock = new Mock<IDateTimeProvider>();
            
            _entitlements.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _clock.Setup(x => x.UtcNow).Returns(new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero));
            
            _service = new TenantAdminReportsService(_repository.Object, _entitlements.Object, _clock.Object, new Mock<E_POS.Application.Modules.Tenant.Reports.Contracts.ITenantAdminReportsAuditLogger>().Object);
        }

        private TenantRequestContext CreateContext(Guid tenantId, Guid userId, params string[] permissions)
        {
            return new TenantRequestContext(tenantId, userId, permissions.ToList());
        }

        [Fact]
        public void CsvGenerator_SanitizesFormulaInjection()
        {
            var records = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "Col1", "=1+1" }, { "Col2", "-2+3" }, { "Col3", "@SUM(A1)" } }
            };

            var bytes = CsvGenerator.Generate(records);
            var csv = Encoding.UTF8.GetString(bytes);
            
            Assert.Contains("\t=1+1", csv);
            Assert.Contains("\t-2+3", csv);
            Assert.Contains("\t@SUM(A1)", csv);
        }

        [Fact]
        public void CsvGenerator_PreservesLeadingZerosForIdentifiers()
        {
            var records = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "OrderId", "00012345" }, { "Number", "0999" } }
            };

            var bytes = CsvGenerator.Generate(records);
            var csv = Encoding.UTF8.GetString(bytes);

            Assert.Contains("00012345", csv);
            Assert.Contains("0999", csv);
        }

        [Fact]
        public void CsvGenerator_EscapesQuotesAndNewlines()
        {
            var records = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "Name", "John \"Doe\"\nNewline" } }
            };

            var bytes = CsvGenerator.Generate(records);
            var csv = Encoding.UTF8.GetString(bytes);

            Assert.Contains("\"John \"\"Doe\"\"\nNewline\"", csv);
        }

        [Fact]
        public async Task CreateExportAsync_IgnoresPageSizeAndFetchesFullDataset()
        {
            var context = CreateContext(Guid.NewGuid(), Guid.NewGuid(), TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView);
            var request = new ReportExportRequest("sales", "unknown_section", "csv", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 2, 10, null, null));
            
            var records = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "id", "1" } }
            };
            var reportResult = new ReportResultDto("unknown_section", "USD", "UTC", null, null, null, null, records, null, DateTimeOffset.UtcNow);
            
            _repository.Setup(x => x.GetSalesAsync(context, It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reportResult);

            var result = await _service.CreateExportAsync(context, request, default);
            
            Assert.True(result.IsSuccess);
            
            _repository.Verify(x => x.GetSalesAsync(context, It.Is<ReportQueryRequest>(req => req.Page == 1 && req.PageSize == int.MaxValue), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateExportAsync_RejectsPdfAndXlsx()
        {
            var context = CreateContext(Guid.NewGuid(), Guid.NewGuid(), TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView);
            var req1 = new ReportExportRequest("sales", "transactions", "pdf", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 25, null, null));
            var req2 = new ReportExportRequest("sales", "transactions", "xlsx", req1.Filters);
            
            var res1 = await _service.CreateExportAsync(context, req1, default);
            var res2 = await _service.CreateExportAsync(context, req2, default);
            
            Assert.False(res1.IsSuccess);
            Assert.Equal("reports.format_not_supported", res1.Error.Code);
            Assert.False(res2.IsSuccess);
            Assert.Equal("reports.format_not_supported", res2.Error.Code);
        }

        [Fact]
        public async Task GetExportAsync_EnforcesIdorAndJobOwnership()
        {
            var tenant1 = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var tenant2 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var ctx1 = CreateContext(tenant1, user1, TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView);
            var ctx2 = CreateContext(tenant2, user2, TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView);
            
            var records = new List<IReadOnlyDictionary<string, object?>> { new Dictionary<string, object?> { { "id", "1" } } };
            _repository.Setup(x => x.GetSalesAsync(ctx1, It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReportResultDto("unknown_section", "USD", "UTC", null, null, null, null, records, null, DateTimeOffset.UtcNow));
            
            var req = new ReportExportRequest("sales", "unknown_section", "csv", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 25, null, null));
            
            var createRes = await _service.CreateExportAsync(ctx1, req, default);
            Assert.True(createRes.IsSuccess);
            var jobId = createRes.Value!.JobId;

            // user1/tenant1 can get
            var getRes1 = await _service.GetExportAsync(ctx1, jobId, default);
            Assert.True(getRes1.IsSuccess);

            // user2/tenant2 cannot get
            var getRes2 = await _service.GetExportAsync(ctx2, jobId, default);
            Assert.False(getRes2.IsSuccess);
            Assert.Equal("reports.not_found", getRes2.Error.Code);
        }
        
        [Fact]
        public async Task DownloadExportAsync_ReturnsCsvData()
        {
            var ctx = CreateContext(Guid.NewGuid(), Guid.NewGuid(), TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView);
            var records = new List<IReadOnlyDictionary<string, object?>> { new Dictionary<string, object?> { { "Foo", "Bar" } } };
            _repository.Setup(x => x.GetSalesAsync(ctx, It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReportResultDto("unknown_section", "USD", "UTC", null, null, null, null, records, null, DateTimeOffset.UtcNow));
            
            var req = new ReportExportRequest("sales", "unknown_section", "csv", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 25, null, null));
            var createRes = await _service.CreateExportAsync(ctx, req, default);
            var jobId = createRes.Value!.JobId;
            
            var dlRes = await _service.DownloadExportAsync(ctx, jobId, default);
            Assert.True(dlRes.IsSuccess);
            
            var csv = Encoding.UTF8.GetString(dlRes.Value!);
            Assert.Contains("Foo", csv);
            Assert.Contains("Bar", csv);
        }
        
        [Fact]
        public async Task CreateExportAsync_EmptyDataset_GeneratesFallbackCsv()
        {
            var ctx = CreateContext(Guid.NewGuid(), Guid.NewGuid(), TenantAdminReportPermissions.Export, TenantAdminReportPermissions.SalesView);
            var records = new List<IReadOnlyDictionary<string, object?>>();
            _repository.Setup(x => x.GetSalesAsync(ctx, It.IsAny<ReportQueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReportResultDto("unknown_section", "USD", "UTC", null, null, null, null, records, null, DateTimeOffset.UtcNow));
            
            var req = new ReportExportRequest("sales", "unknown_section", "csv", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 1, 25, null, null));
            var createRes = await _service.CreateExportAsync(ctx, req, default);
            var jobId = createRes.Value!.JobId;
            
            var dlRes = await _service.DownloadExportAsync(ctx, jobId, default);
            Assert.True(dlRes.IsSuccess);
            
            var csv = Encoding.UTF8.GetString(dlRes.Value!);
            Assert.Contains("No Results", csv);
        }
    }
}









