using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingExportIntegrationTests
    {
        private DbContextOptions<EPosDbContext> CreateOptions(string dbName) =>
            new DbContextOptionsBuilder<EPosDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

        private T Create<T>(params (string, object)[] props) where T : new()
        {
            var obj = new T();
            foreach (var p in props)
            {
                var type = typeof(T);
                PropertyInfo? propInfo = null;
                while (type != null && propInfo == null)
                {
                    propInfo = type.GetProperty(p.Item1, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                    type = type.BaseType;
                }
                if (propInfo != null)
                {
                    propInfo.SetValue(obj, p.Item2);
                }
            }
            return obj;
        }

        private TenantUser CreateUser(Guid id, Guid tenantId, string status, string outletScope, string tillScope) =>
            Create<TenantUser>(("Id", id), ("TenantId", tenantId), ("AccountStatus", status), ("OutletAccessScope", outletScope), ("TillAccessScope", tillScope), ("UpdatedAt", DateTimeOffset.UtcNow), ("StaffCode", "TEST"));
            
        private TenantRequestContext CreateContext(Guid tenantId, Guid userId, params string[] permissions) =>
            new TenantRequestContext(tenantId, userId, permissions.ToList());

        [Fact]
        public async Task Export_GreaterThanPageSize_FiltersProperlyAndMaintainsSecurity()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var outletId = Guid.NewGuid();
            var options = CreateOptions(Guid.NewGuid().ToString());
            
            await using (var db = new EPosDbContext(options))
            {
                db.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tenantId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                db.TenantUsers.Add(CreateUser(userId, tenantId, "ACTIVE", "ALL_OUTLETS", "ALL_ACCESSIBLE_TILLS"));
                db.Outlets.Add(Create<Outlet>(("Id", outletId), ("TenantId", tenantId), ("Name", "Test"), ("Status", "ACTIVE"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                db.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tenantId), ("TenantId", tenantId), ("Name", "POS"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                
                // Add 60 sales orders
                for (int i = 0; i < 60; i++)
                {
                    db.SalesOrders.Add(Create<SalesOrder>(("Id", Guid.NewGuid()), ("TenantId", tenantId), ("OrderNumber", $"ORD-{i:D3}"),
                        ("ReportingOutletId", outletId), ("OrderStatus", "COMPLETED"), ("UpdatedAt", DateTimeOffset.UtcNow), ("CompletedAt", DateTimeOffset.UtcNow), ("SalesChannelId", tenantId)));
                }
                
                await db.SaveChangesAsync();
            }
            
            await using (var db = new EPosDbContext(options))
            {
                var repo = new TenantAdminReportsRepository(db);
                var entitlementsMock = new Mock<ITenantFeatureEntitlementEvaluator>();
                entitlementsMock.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
                
                var clockMock = new Mock<IDateTimeProvider>();
                clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
                
                var auditLoggerMock = new Mock<ITenantAdminReportsAuditLogger>();
                
                var service = new TenantAdminReportsService(repo, entitlementsMock.Object, clockMock.Object, auditLoggerMock.Object);
                var context = CreateContext(tenantId, userId, TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
                
                // Screen = 25 records
                var screenRequest = new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions", 1, 25);
                var screenResult = await service.GetSalesAsync(context, screenRequest, CancellationToken.None);
                
                Assert.True(screenResult.IsSuccess);
                Assert.Equal(25, screenResult.Value!.Records.Count);
                Assert.Equal(60, screenResult.Value.Pagination!.TotalCount);
                
                // Export = 60 records
                var exportRequest = new ReportExportRequest("sales", "transactions", "csv", screenRequest);
                var createResult = await service.CreateExportAsync(context, exportRequest, CancellationToken.None);
                
                Assert.True(createResult.IsSuccess);
                var jobId = createResult.Value!.JobId;
                
                var downloadResult = await service.DownloadExportAsync(context, jobId, CancellationToken.None);
                Assert.True(downloadResult.IsSuccess);
                
                var csv = Encoding.UTF8.GetString(downloadResult.Value!);
                var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // 8 metadata + 1 header + 60 records = 69 lines
                Assert.Equal(69, lines.Length);
                
                auditLoggerMock.Verify(x => x.LogExportJobCreatedAsync(tenantId, userId, jobId, exportRequest, It.IsAny<CancellationToken>()), Times.Once);
                auditLoggerMock.Verify(x => x.LogExportDownloadedAsync(tenantId, userId, jobId, It.IsAny<CancellationToken>()), Times.Once);
            }
        }
        
        [Fact]
        public async Task Expiry_Enforced_Correctly()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var options = CreateOptions(Guid.NewGuid().ToString());
            
            await using (var db = new EPosDbContext(options))
            {
                db.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tenantId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                db.TenantUsers.Add(CreateUser(userId, tenantId, "ACTIVE", "ALL_OUTLETS", "ALL_ACCESSIBLE_TILLS"));
                await db.SaveChangesAsync();
            }
            
            await using (var db = new EPosDbContext(options))
            {
                var repo = new TenantAdminReportsRepository(db);
                var entitlementsMock = new Mock<ITenantFeatureEntitlementEvaluator>();
                entitlementsMock.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
                
                var time = DateTimeOffset.UtcNow;
                var clockMock = new Mock<IDateTimeProvider>();
                clockMock.Setup(x => x.UtcNow).Returns(() => time); // Dynamic return
                
                var auditLoggerMock = new Mock<ITenantAdminReportsAuditLogger>();
                
                var service = new TenantAdminReportsService(repo, entitlementsMock.Object, clockMock.Object, auditLoggerMock.Object);
                var context = CreateContext(tenantId, userId, TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
                
                var request = new ReportExportRequest("sales", "transactions", "csv", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"));
                var createResult = await service.CreateExportAsync(context, request, CancellationToken.None);
                
                var jobId = createResult.Value!.JobId;
                
                // Advance time by 16 minutes
                time = time.AddMinutes(16);
                
                var downloadResult = await service.DownloadExportAsync(context, jobId, CancellationToken.None);
                Assert.False(downloadResult.IsSuccess);
                Assert.Equal("reports.not_found", downloadResult.Error!.Code);
            }
        }
        
        [Fact]
        public async Task Export_Idor_CrossTenant_And_CrossUser_Blocks()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var options = CreateOptions(Guid.NewGuid().ToString());
            
            await using (var db = new EPosDbContext(options))
            {
                db.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tenantId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow)));
                db.TenantUsers.Add(CreateUser(userId, tenantId, "ACTIVE", "ALL_OUTLETS", "ALL_ACCESSIBLE_TILLS"));
                await db.SaveChangesAsync();
            }
            
            await using (var db = new EPosDbContext(options))
            {
                var repo = new TenantAdminReportsRepository(db);
                var entitlementsMock = new Mock<ITenantFeatureEntitlementEvaluator>();
                entitlementsMock.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
                var clockMock = new Mock<IDateTimeProvider>();
                clockMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
                var auditLoggerMock = new Mock<ITenantAdminReportsAuditLogger>();
                
                var service = new TenantAdminReportsService(repo, entitlementsMock.Object, clockMock.Object, auditLoggerMock.Object);
                var context = CreateContext(tenantId, userId, TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
                
                var request = new ReportExportRequest("sales", "transactions", "csv", new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"));
                var createResult = await service.CreateExportAsync(context, request, CancellationToken.None);
                
                var jobId = createResult.Value!.JobId;
                
                // Different user, same tenant
                var diffUserContext = CreateContext(tenantId, Guid.NewGuid(), TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
                var downloadDiffUser = await service.DownloadExportAsync(diffUserContext, jobId, CancellationToken.None);
                Assert.False(downloadDiffUser.IsSuccess);
                
                // Different tenant, same user
                var diffTenantContext = CreateContext(Guid.NewGuid(), userId, TenantAdminReportPermissions.SalesView, TenantAdminReportPermissions.Export);
                var downloadDiffTenant = await service.DownloadExportAsync(diffTenantContext, jobId, CancellationToken.None);
                Assert.False(downloadDiffTenant.IsSuccess);
            }
        }
    }
}





