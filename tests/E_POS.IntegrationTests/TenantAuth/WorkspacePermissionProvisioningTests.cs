using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.TenantAuth;

public sealed class WorkspacePermissionProvisioningTests
{
    [LocalWorkspaceFact]
    public async Task BackfillTwicePreservesRowsAndResolvesExistingCashier()
    {
        var connection = Environment.GetEnvironmentVariable("WORKSPACE_TEST_CONNECTION")!;
        Assert.Contains(new NpgsqlConnectionStringBuilder(connection).Host, new[] { "localhost", "127.0.0.1" });
        await using var db = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connection).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(WorkspacePermissionSeedData.UpSql);
        var definitions = await db.PermissionDefinitions.CountAsync();
        var grants = await db.TenantRolePermissions.CountAsync();
        await db.Database.ExecuteSqlRawAsync(WorkspacePermissionSeedData.UpSql);
        Assert.Equal(definitions, await db.PermissionDefinitions.CountAsync());
        Assert.Equal(grants, await db.TenantRolePermissions.CountAsync());
        var user = await db.TenantUsers.SingleAsync(x => x.Email == "CASHIER001@GMAIL.COM");
        var repository = new TenantAuthRepository(db, NullLogger<TenantAuthRepository>.Instance);
        var codes = await repository.GetActivePermissionCodesAsync(user.Id, user.TenantId, default);
        Assert.Contains("workspace.pos.access", codes);
        Assert.Contains("pos.till.open", codes);
        Assert.DoesNotContain("workspace.tenant_admin.access", codes);
        var admin = await db.TenantUsers.SingleAsync(x => x.Email == "TENANTADMIN001@GMAIL.COM");
        var adminCodes = await repository.GetActivePermissionCodesAsync(admin.Id, admin.TenantId, default);
        Assert.Contains("workspace.tenant_admin.access", adminCodes);
        Assert.DoesNotContain("workspace.pos.access", adminCodes);
        Assert.Contains("tenant.dashboard.view", adminCodes);
        await transaction.RollbackAsync();
    }

    public sealed class LocalWorkspaceFactAttribute : FactAttribute
    {
        public LocalWorkspaceFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WORKSPACE_TEST_CONNECTION")))
                Skip = "Opt-in local Development DB verification only.";
        }
    }
}
