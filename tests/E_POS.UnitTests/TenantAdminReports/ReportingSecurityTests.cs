using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Application.Common.Models;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingSecurityTests
    {
        private static DbContextOptions<EPosDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<EPosDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private static void SetProp(object obj, string name, object value)
        {
            obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(obj, value);
        }

        [Fact]
        public async Task AllOutletsScope_ReturnsAllTenantOutlets()
        {
            var options = CreateNewContextOptions();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            using (var context = new EPosDbContext(options))
            {
                var user = new TenantUser();
                SetProp(user, "Id", userId);
                SetProp(user, "TenantId", tenantId); SetProp(user, "StaffCode", "STAFF"); SetProp(user, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(user, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(user, "OutletAccessScope", TenantUserAccessScopes.AllOutlets);
                
                var tenant = new Tenant();
                SetProp(tenant, "Id", tenantId);
                SetProp(tenant, "TenantName", "Test");
                SetProp(tenant, "Timezone", "UTC");
                SetProp(tenant, "CurrencyCode", "USD");
                SetProp(tenant, "Status", "ACTIVE");
                SetProp(tenant, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(tenant, "UpdatedAt", DateTimeOffset.UtcNow);
                context.Tenants.Add(tenant);
                context.TenantUsers.Add(user);

                var a1 = new Outlet();
                SetProp(a1, "Id", Guid.NewGuid());
                SetProp(a1, "TenantId", tenantId); SetProp(a1, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(a1, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(a1, "OutletName", "A1");
                SetProp(a1, "OutletCode", "A1");
                SetProp(a1, "Status", "ACTIVE");
                context.Outlets.Add(a1);

                var a2 = new Outlet();
                SetProp(a2, "Id", Guid.NewGuid());
                SetProp(a2, "TenantId", tenantId); SetProp(a2, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(a2, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(a2, "OutletName", "A2");
                SetProp(a2, "OutletCode", "A2");
                SetProp(a2, "Status", "ACTIVE");
                context.Outlets.Add(a2);

                var b1 = new Outlet();
                SetProp(b1, "Id", Guid.NewGuid());
                SetProp(b1, "TenantId", Guid.NewGuid()); SetProp(b1, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(b1, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(b1, "OutletName", "B1");
                SetProp(b1, "OutletCode", "B1");
                SetProp(b1, "Status", "ACTIVE");
                context.Outlets.Add(b1);

                await context.SaveChangesAsync();
            }

            using (var context = new EPosDbContext(options))
            {
                var repository = new TenantAdminReportsRepository(context);
                var reqContext = new TenantRequestContext(tenantId, userId, new List<string>());

                var result = await repository.GetFilterOptionsAsync(reqContext, new ReportFilterOptionsRequest(null, null, null, null, null, null, false, 1, 25), CancellationToken.None);

                Assert.Equal(2, result.Groups["outlets"].Count);
                Assert.Contains(result.Groups["outlets"], o => o.Name == "A1");
                Assert.Contains(result.Groups["outlets"], o => o.Name == "A2");
                Assert.DoesNotContain(result.Groups["outlets"], o => o.Name == "B1");
            }
        }

        [Fact]
        public async Task ExplicitOutletAccess_ReturnsOnlyAssignedOutlets()
        {
            var options = CreateNewContextOptions();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var outletA1Id = Guid.NewGuid();
            var outletA2Id = Guid.NewGuid();

            using (var context = new EPosDbContext(options))
            {
                var user = new TenantUser();
                SetProp(user, "Id", userId);
                SetProp(user, "TenantId", tenantId); SetProp(user, "StaffCode", "STAFF"); SetProp(user, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(user, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(user, "OutletAccessScope", TenantUserAccessScopes.SelectedOutlets);
                
                var tenant = new Tenant();
                SetProp(tenant, "Id", tenantId);
                SetProp(tenant, "TenantName", "Test");
                SetProp(tenant, "Timezone", "UTC");
                SetProp(tenant, "CurrencyCode", "USD");
                SetProp(tenant, "Status", "ACTIVE");
                SetProp(tenant, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(tenant, "UpdatedAt", DateTimeOffset.UtcNow);
                context.Tenants.Add(tenant);
                context.TenantUsers.Add(user);

                var a1 = new Outlet();
                SetProp(a1, "Id", outletA1Id);
                SetProp(a1, "TenantId", tenantId); SetProp(a1, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(a1, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(a1, "OutletName", "A1");
                SetProp(a1, "OutletCode", "A1");
                SetProp(a1, "Status", "ACTIVE");
                context.Outlets.Add(a1);

                var a2 = new Outlet();
                SetProp(a2, "Id", outletA2Id);
                SetProp(a2, "TenantId", tenantId); SetProp(a2, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(a2, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(a2, "OutletName", "A2");
                SetProp(a2, "OutletCode", "A2");
                SetProp(a2, "Status", "ACTIVE");
                context.Outlets.Add(a2);

                var b1 = new Outlet();
                SetProp(b1, "Id", Guid.NewGuid());
                SetProp(b1, "TenantId", Guid.NewGuid()); SetProp(b1, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(b1, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(b1, "OutletName", "B1");
                SetProp(b1, "OutletCode", "B1");
                SetProp(b1, "Status", "ACTIVE");
                context.Outlets.Add(b1);

                var role = new OutletUserRole();
                SetProp(role, "Id", Guid.NewGuid());
                SetProp(role, "TenantId", tenantId);
                SetProp(role, "TenantUserId", userId); SetProp(role, "UpdatedAt", DateTimeOffset.UtcNow); SetProp(role, "CreatedAt", DateTimeOffset.UtcNow);
                SetProp(role, "OutletId", outletA1Id);
                context.OutletUserRoles.Add(role);

                await context.SaveChangesAsync();
            }

            using (var context = new EPosDbContext(options))
            {
                var repository = new TenantAdminReportsRepository(context);
                var reqContext = new TenantRequestContext(tenantId, userId, new List<string>());

                var result = await repository.GetFilterOptionsAsync(reqContext, new ReportFilterOptionsRequest(null, null, null, null, null, null, false, 1, 25), CancellationToken.None);

                var outlet = Assert.Single(result.Groups["outlets"]);
                Assert.Equal("A1", outlet.Name);
            }
        }
    }
}




