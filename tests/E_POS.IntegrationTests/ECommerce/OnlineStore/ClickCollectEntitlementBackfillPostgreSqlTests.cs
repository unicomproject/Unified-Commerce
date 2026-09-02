using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.OnlineStore;

public sealed class ClickCollectEntitlementBackfillPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task Migration_BackfillsOnlyMissingClickCollectForEnabledOnlineStoreTenants()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        var databaseName = $"click_collect_entitlement_backfill_{Guid.NewGuid():N}";
        var adminConnectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString) { Database = "postgres" }.ConnectionString;
        var connectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            Database = databaseName,
            IncludeErrorDetail = true,
        }.ConnectionString;

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var baseline = new NpgsqlCommand(
                """
                CREATE TABLE tenants (id uuid PRIMARY KEY);
                CREATE TABLE platform_features (
                    id uuid PRIMARY KEY,
                    feature_code text NOT NULL,
                    status text NOT NULL);
                CREATE TABLE tenant_feature_entitlements (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    platform_feature_id uuid NOT NULL,
                    feature_id uuid NOT NULL,
                    entitlement_status text NOT NULL,
                    source_type text NOT NULL,
                    is_enabled boolean NOT NULL,
                    effective_from timestamptz NOT NULL,
                    effective_until timestamptz NULL,
                    revoked_at timestamptz NULL,
                    created_at timestamptz NOT NULL,
                    updated_at timestamptz NOT NULL,
                    UNIQUE (tenant_id, platform_feature_id));
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" varchar(150) PRIMARY KEY,
                    "ProductVersion" varchar(32) NOT NULL);
                """,
                connection))
            {
                await baseline.ExecuteNonQueryAsync();
            }

            var enabledTenant = Guid.NewGuid();
            var disabledTenant = Guid.NewGuid();
            var explicitlyDisabledClickCollectTenant = Guid.NewGuid();
            var onlineStoreFeature = Guid.NewGuid();
            var clickCollectFeature = Guid.NewGuid();
            await using (var seed = new NpgsqlCommand(
                """
                INSERT INTO tenants(id) VALUES (@enabled), (@disabled), (@explicitlyDisabled);
                INSERT INTO platform_features(id, feature_code, status)
                VALUES (@onlineStore, 'online_store', 'ACTIVE'),
                       (@clickCollect, 'click_collect', 'ACTIVE');

                INSERT INTO tenant_feature_entitlements(
                    id, tenant_id, platform_feature_id, feature_id, entitlement_status,
                    source_type, is_enabled, effective_from, effective_until, revoked_at,
                    created_at, updated_at)
                VALUES
                    (gen_random_uuid(), @enabled, @onlineStore, @onlineStore, 'ENABLED', 'MANUAL', TRUE, now(), NULL, NULL, now(), now()),
                    (gen_random_uuid(), @disabled, @onlineStore, @onlineStore, 'DISABLED', 'MANUAL', FALSE, now(), NULL, NULL, now(), now()),
                    (gen_random_uuid(), @explicitlyDisabled, @onlineStore, @onlineStore, 'ENABLED', 'MANUAL', TRUE, now(), NULL, NULL, now(), now()),
                    (gen_random_uuid(), @explicitlyDisabled, @clickCollect, @clickCollect, 'DISABLED', 'MANUAL', FALSE, now(), NULL, NULL, now(), now());
                """,
                connection))
            {
                seed.Parameters.AddWithValue("enabled", enabledTenant);
                seed.Parameters.AddWithValue("disabled", disabledTenant);
                seed.Parameters.AddWithValue("explicitlyDisabled", explicitlyDisabledClickCollectTenant);
                seed.Parameters.AddWithValue("onlineStore", onlineStoreFeature);
                seed.Parameters.AddWithValue("clickCollect", clickCollectFeature);
                await seed.ExecuteNonQueryAsync();
            }

            await using var db = new EPosDbContext(
                new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);
            var script = db.Database.GetService<IMigrator>().GenerateScript(
                "20260827120000_HardenTenantAdminOnlineStoreSlugUniqueness",
                "20260902120000_BackfillClickCollectEntitlementForOnlineStoreTenants");
            await using (var migrate = new NpgsqlCommand(script, connection))
            {
                await migrate.ExecuteNonQueryAsync();
            }

            await using var assertCommand = new NpgsqlCommand(
                """
                SELECT tenant_id, entitlement_status, is_enabled
                FROM tenant_feature_entitlements
                WHERE platform_feature_id = @clickCollect
                ORDER BY tenant_id;
                """,
                connection);
            assertCommand.Parameters.AddWithValue("clickCollect", clickCollectFeature);
            await using var reader = await assertCommand.ExecuteReaderAsync();
            var rows = new Dictionary<Guid, (string Status, bool Enabled)>();
            while (await reader.ReadAsync())
            {
                rows.Add(reader.GetGuid(0), (reader.GetString(1), reader.GetBoolean(2)));
            }

            Assert.Equal(2, rows.Count);
            Assert.Equal(("ENABLED", true), rows[enabledTenant]);
            Assert.Equal(("DISABLED", false), rows[explicitlyDisabledClickCollectTenant]);
            Assert.DoesNotContain(disabledTenant, rows.Keys);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
                             admin))
            {
                terminate.Parameters.AddWithValue("database", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(BaseConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
