using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.Reports;

/// <summary>
/// Reporting Release 1 on real PostgreSQL: the full migration chain (including the reporting
/// permission seed) applies to a clean database, and every report section's LINQ translates and
/// executes through Npgsql inside the RepeatableRead snapshot transaction.
/// </summary>
public sealed class TenantAdminReportsPostgreSqlTests
{
    private static readonly string AdminConnectionString =
        new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=SNirosh1985")
        { Database = "postgres" }.ConnectionString;

    private const string SeedTenantId = "55555555-0000-4000-8000-000000000001";

    [Fact]
    public async Task CleanMigration_SeedsReportPermissions_AndEveryReportSectionExecutesOnPostgreSql()
    {
        var databaseName = $"reports_r1_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(AdminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = databaseName }.ConnectionString;
            await using var db = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_SeedTenantReportingPermissions", StringComparison.Ordinal));

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            async Task<long> Count(string sql)
            {
                await using var command = new NpgsqlCommand(sql, connection);
                return (long)(await command.ExecuteScalarAsync())!;
            }
            var definedReportPermissions = await Count(
                "SELECT count(*) FROM permission_definitions WHERE permission_code LIKE 'tenant.reports.%'");
            Assert.Equal(1, await Count("SELECT count(*) FROM permission_definitions WHERE permission_code = 'tenant.reports.sales.view'"));
            {
                Assert.Equal(TenantAdminReportPermissions.All.Count, definedReportPermissions);
                Assert.Equal(1, await Count("""
                    SELECT count(*) FROM tenant_role_permissions trp
                    JOIN tenant_roles tr ON tr.id = trp.role_id
                    JOIN permission_definitions pd ON pd.id = trp.permission_id
                    WHERE tr.role_code = 'TENANT_ADMIN' AND tr.tenant_id = '55555555-0000-4000-8000-000000000001'
                      AND pd.permission_code = 'tenant.reports.export'
                    """));
                Assert.Equal(0, await Count("""
                    SELECT count(*) FROM tenant_role_permissions trp
                    JOIN tenant_roles tr ON tr.id = trp.role_id
                    JOIN permission_definitions pd ON pd.id = trp.permission_id
                    WHERE tr.role_code IN ('STORE_MANAGER', 'CASHIER')
                      AND pd.permission_code IN ('tenant.reports.export', 'tenant.reports.customer-pii.view')
                    """));
            }

            var tenantId = Guid.Parse(SeedTenantId);
            var userId = await db.TenantUsers.AsNoTracking().Where(x => x.TenantId == tenantId).Select(x => x.Id).FirstOrDefaultAsync();
            var context = new TenantRequestContext(tenantId, userId == Guid.Empty ? Guid.NewGuid() : userId, TenantAdminReportPermissions.All.ToArray());
            var repository = new TenantAdminReportsRepository(db);
            var day = new DateOnly(2026, 9, 20);
            ReportQueryRequest Query(string section) => new(day, day, null, null, null, null, null, Guid.NewGuid(), null, null, null, null, null, null,
                null, null, "x", section, 1, 25, MovementType: null);

            foreach (var section in new[] { "summary", "transactions", "channels", "tax", "payments", "payment-transactions", "online",
                         "collections", "returns", "products", "categories", "discounts", "cashiers", "daily" })
            {
                var result = await repository.GetSalesAsync(context, Query(section) with { CategoryId = section == "products" ? Guid.NewGuid() : null }, default);
                Assert.NotNull(result.Summary);
                Assert.NotNull(result.KnownPendingSyncCount);
            }
            foreach (var section in new[] { "current", "movements", "low-stock", "valuation" })
                Assert.NotNull((await repository.GetStockAsync(context, Query(section) with { BatchNumber = "B1" }, default)).Summary);
            foreach (var section in new[] { "tills", "performance" })
                Assert.NotNull((await repository.GetOutletsAsync(context, Query(section), default)).Summary);
            Assert.NotNull((await repository.GetDashboardAsync(context, Query("dashboard"), default)).Sections);
            var options = await repository.GetFilterOptionsAsync(context, new(null, null, null, null, null, null), default);
            Assert.Contains("returnReasons", options.Groups.Keys);
            Assert.Null(await repository.GetSalesTransactionDetailAsync(context, Guid.NewGuid(), default));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(AdminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()", admin))
            {
                terminate.Parameters.AddWithValue("database", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
