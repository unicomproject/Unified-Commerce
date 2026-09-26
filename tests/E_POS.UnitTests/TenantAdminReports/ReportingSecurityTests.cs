using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Common.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingSecurityTests
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
                PropertyInfo propInfo = null;
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

        private Outlet CreateOutlet(Guid id, Guid tenantId, string status = "ACTIVE") =>
            Create<Outlet>(("Id", id), ("TenantId", tenantId), ("Name", "Test"), ("Status", status), ("UpdatedAt", DateTimeOffset.UtcNow));

        private Till CreateTill(Guid id, Guid tenantId, Guid outletId) =>
            Create<Till>(("Id", id), ("TenantId", tenantId), ("OutletId", outletId), ("Name", "Test"), ("Status", "ACTIVE"), ("UpdatedAt", DateTimeOffset.UtcNow));

        private SalesOrder CreateOrder(Guid id, Guid tenantId, Guid outletId, Guid? tillId = null, string status = "COMPLETED") =>
            Create<SalesOrder>(("Id", id), ("TenantId", tenantId), ("ReportingOutletId", outletId), ("TillId", (object)tillId), ("OrderNumber", id.ToString()), ("Status", status), ("PaymentStatus", "PAID"), ("CompletedAt", DateTimeOffset.UtcNow), ("UpdatedAt", DateTimeOffset.UtcNow), ("SalesChannelId", tenantId));

        // Test 1
        [Fact]
        public async Task AllOutletsScope_ReturnsAllTenantOutlets()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            var o2 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.AddRange(o1, o2);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.AllAccessibleTills));
            
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o2.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Equal(2, result.Records.Count);
        }

        // Test 2
        [Fact]
        public async Task ExplicitOutletAccess_ReturnsOnlyAssignedOutlets()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            var o2 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.AddRange(o1, o2);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.SelectedOutlets, TenantUserAccessScopes.AllAccessibleTills));
            ctx.OutletUserRoles.Add(Create<OutletUserRole>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("OutletId", o1.Id)));
            
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o2.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Single(result.Records);
        }

        // Test 3
        [Fact]
        public async Task SelectedOutlets_WithNoAssignments_ReturnsNoData()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.SelectedOutlets, TenantUserAccessScopes.AllAccessibleTills));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 4
        [Fact]
        public async Task NoOutletAccess_ReturnsNoData()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.NoOutletAccess, TenantUserAccessScopes.AllAccessibleTills));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 5
        [Fact]
        public async Task MissingTenantUser_FailsClosed()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            await ctx.SaveChangesAsync();
            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 6
        [Fact]
        public async Task InactiveTenantUser_FailsClosed()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "INACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.AllAccessibleTills));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records); 
        }

        // Test 7
        [Fact]
        public async Task AllAccessibleTills_ReturnsOnlyTillsInsideAccessibleOutlets()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            var o2 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.AddRange(o1, o2);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.SelectedOutlets, TenantUserAccessScopes.AllAccessibleTills));
            ctx.OutletUserRoles.Add(Create<OutletUserRole>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("OutletId", o1.Id)));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o2.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o2.Id, t2.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Single(result.Records);
        }

        // Test 8
        [Fact]
        public async Task SelectedTills_ReturnsOnlyAssignedTills()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t2.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Single(result.Records);
        }

        // Test 9
        [Fact]
        public async Task SelectedTills_WithNoAssignments_ReturnsEmpty()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.Add(t1);
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 10
        [Fact]
        public async Task NoTillAccess_ReturnsEmpty()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.NoTillAccess));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.Add(t1);
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 11
        [Fact]
        public async Task TillOutsideAccessibleOutlet_IsRejected()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId); // Accessible
            var o2 = CreateOutlet(Guid.NewGuid(), tId); // Not accessible
            ctx.Outlets.AddRange(o1, o2);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.SelectedOutlets, TenantUserAccessScopes.SelectedTills));
            ctx.OutletUserRoles.Add(Create<OutletUserRole>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("OutletId", o1.Id)));
            
            var t1 = CreateTill(Guid.NewGuid(), tId, o2.Id); // Inside non-accessible outlet
            ctx.Tills.Add(t1);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id))); 
            
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o2.Id, t1.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 12
        [Fact]
        public async Task SalesReport_EmptyAccess_ReturnsZeroSales()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.NoOutletAccess, TenantUserAccessScopes.AllAccessibleTills));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, Guid.NewGuid(), null, "COMPLETED"));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 13
        [Fact]
        public async Task SalesReport_RestrictedTill_ExcludesUnauthorizedSales()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId, o1.Id, t2.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Single(result.Records);
        }

        // Test 14
        [Fact]
        public async Task PaymentReport_PaymentQuery_ExcludesUnauthorizedSales()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            
            var oT1 = CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id);
            var oT2 = CreateOrder(Guid.NewGuid(), tId, o1.Id, t2.Id);
            ctx.SalesOrders.AddRange(oT1, oT2);
            
            ctx.SalesPayments.Add(Create<SalesPayment>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("SalesOrderId", oT1.Id), ("Amount", 10m), ("Status", "COMPLETED")));
            ctx.SalesPayments.Add(Create<SalesPayment>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("SalesOrderId", oT2.Id), ("Amount", 10m), ("Status", "COMPLETED")));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Single(result.Records);
        }

        // Test 15
        [Fact]
        public async Task ProductSales_RestrictedTill_ExcludesUnauthorizedSales()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            
            var oT1 = CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id);
            var oT2 = CreateOrder(Guid.NewGuid(), tId, o1.Id, t2.Id);
            ctx.SalesOrders.AddRange(oT1, oT2);
            
            ctx.SalesOrderLines.Add(Create<SalesOrderLine>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("SalesOrderId", oT1.Id), ("Quantity", 1m)));
            ctx.SalesOrderLines.Add(Create<SalesOrderLine>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("SalesOrderId", oT2.Id), ("Quantity", 1m)));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Single(result.Records);
        }

        // Test 16
        [Fact]
        public async Task FilterOptions_DataLeak_ExcludesUnauthorizedTills()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var req = new ReportFilterOptionsRequest(null, null, null, null, null, "tills", false, 1, 25);
            var result = await repo.GetFilterOptionsAsync(new TenantRequestContext(tId, uId, new List<string>()), req, CancellationToken.None);
            
            Assert.Single(result.Groups["tills"]);
        }

        // Test 17
        [Fact]
        public async Task SalesTransactionDetail_IDOR_FailsClosed()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            
            var oT2 = CreateOrder(Guid.NewGuid(), tId, o1.Id, t2.Id); // Unauthorized order for uId
            ctx.SalesOrders.Add(oT2);
            
            var tId3 = Guid.NewGuid(); // Another tenant
            var oT3 = CreateOrder(Guid.NewGuid(), tId3, Guid.NewGuid());
            ctx.SalesOrders.Add(oT3);

            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesTransactionDetailAsync(new TenantRequestContext(tId, uId, new List<string>()), oT2.Id, CancellationToken.None);
            Assert.Null(result); // IDOR rejected (Same tenant / unauthorized Till)
            
            var result2 = await repo.GetSalesTransactionDetailAsync(new TenantRequestContext(tId, uId, new List<string>()), oT3.Id, CancellationToken.None);
            Assert.Null(result2); // IDOR rejected (Cross Tenant)
        }

        // Test 18
        [Fact]
        public async Task CrossTenantUser_CannotSeeOtherTenantOutlets()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId1 = Guid.NewGuid(); var tId2 = Guid.NewGuid(); var uId = Guid.NewGuid(); ctx.Tenants.AddRange(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId1), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow)), Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId2), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow)));
            var o2 = CreateOutlet(Guid.NewGuid(), tId2);
            ctx.Outlets.Add(o2);
            ctx.TenantUsers.Add(CreateUser(uId, tId1, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.AllAccessibleTills));
            ctx.SalesOrders.Add(CreateOrder(Guid.NewGuid(), tId2, o2.Id));
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId1, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            Assert.Empty(result.Records);
        }

        // Test 19
        [Fact]
        public async Task Dashboard_TillSession_ExcludesUnauthorizedSessions()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); 
            var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            
            // user allowed only Till A1
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            var t2 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.AddRange(t1, t2);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            
            var s1 = Create<TillSession>(("Id", Guid.NewGuid()), ("TenantId", tId), ("TillId", t1.Id), ("OutletId", o1.Id), ("Status", "OPEN"), ("OpenedAt", DateTimeOffset.UtcNow), ("UpdatedAt", DateTimeOffset.UtcNow));
            var s2 = Create<TillSession>(("Id", Guid.NewGuid()), ("TenantId", tId), ("TillId", t2.Id), ("OutletId", o1.Id), ("Status", "OPEN"), ("OpenedAt", DateTimeOffset.UtcNow), ("UpdatedAt", DateTimeOffset.UtcNow));
            ctx.TillSessions.AddRange(s1, s2);
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetOutletsAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "tills"), CancellationToken.None);
            
            // Assert that only session for t1 is accessible (represented by the returned records)
            Assert.Single(result.Records);
        }

        // Test 20
        [Fact]
        public async Task SelectedTillScope_NullTillOrder_FollowsApprovedScopeRule()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = new EPosDbContext(CreateOptions(db));
            var tId = Guid.NewGuid(); ctx.Tenants.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant>(("Id", tId), ("Name", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var uId = Guid.NewGuid();
            ctx.SalesChannels.Add(Create<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.SalesChannel>(("Id", tId), ("TenantId", tId), ("ChannelName", "Test"), ("UpdatedAt", DateTimeOffset.UtcNow))); var o1 = CreateOutlet(Guid.NewGuid(), tId);
            ctx.Outlets.Add(o1);
            ctx.TenantUsers.Add(CreateUser(uId, tId, "ACTIVE", TenantUserAccessScopes.AllOutlets, TenantUserAccessScopes.SelectedTills));
            var t1 = CreateTill(Guid.NewGuid(), tId, o1.Id);
            ctx.Tills.Add(t1);
            ctx.TenantUserTillAccess.Add(Create<TenantUserTillAccess>(("Id", Guid.NewGuid()), ("TenantId", tId), ("UpdatedAt", DateTimeOffset.UtcNow), ("TenantUserId", uId), ("TillId", t1.Id)));
            
            var oT1 = CreateOrder(Guid.NewGuid(), tId, o1.Id, t1.Id);
            var oNull = CreateOrder(Guid.NewGuid(), tId, o1.Id, null); // Online order
            ctx.SalesOrders.AddRange(oT1, oNull);
            await ctx.SaveChangesAsync();

            var repo = new TenantAdminReportsRepository(ctx);
            var result = await repo.GetSalesAsync(new TenantRequestContext(tId, uId, new List<string>()), new ReportQueryRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "transactions"), CancellationToken.None);
            
            // Expected: Only oT1 is visible. oNull (null till) is hidden because user has SELECTED_TILLS.
            Assert.Single(result.Records);
        }
    }
}














