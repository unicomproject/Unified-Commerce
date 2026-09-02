using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.AccessControl;

public sealed class TenantUserAccessMigrationModelAlignmentTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public void CurrentModel_HasNoOperationsBeyondMigrationSnapshot()
    {
        using var db = new EPosDbContext(
            new DbContextOptionsBuilder<EPosDbContext>()
                .UseNpgsql("Host=localhost;Database=model_alignment;Username=postgres;Password=admin")
                .Options);

        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(migrationsAssembly.ModelSnapshot);
        var snapshotModel = snapshot.Model;
        if (snapshotModel is IMutableModel mutableSnapshot)
        {
            snapshotModel = mutableSnapshot.FinalizeModel();
        }

        snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);
        var currentModel = db.GetService<IDesignTimeModel>().Model;
        var operations = db.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.True(
            operations.Count == 0,
            $"Pending model operations: {string.Join(", ", operations.Select(Describe))}");
    }

    [Fact]
    public async Task ExplicitOutletTillAccessMigration_AppliesAndBackfillsLegacySelectedOutletUsers_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        var databaseName = $"tenant_user_access_migration_{Guid.NewGuid():N}";
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
            var tenantId = Guid.NewGuid();
            var selectedUserId = Guid.NewGuid();
            var tenantWideUserId = Guid.NewGuid();
            var outletId = Guid.NewGuid();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var baseline = new NpgsqlCommand(
                """
                CREATE TABLE tenants (id uuid PRIMARY KEY);
                CREATE TABLE tenant_users (id uuid PRIMARY KEY, tenant_id uuid NOT NULL REFERENCES tenants(id));
                CREATE TABLE tills (id uuid PRIMARY KEY);
                CREATE TABLE tenant_user_roles (tenant_id uuid NOT NULL, user_id uuid NOT NULL, revoked_at timestamptz NULL);
                CREATE TABLE outlet_user_roles (tenant_id uuid NOT NULL, outlet_id uuid NOT NULL, user_id uuid NOT NULL, revoked_at timestamptz NULL);
                CREATE TABLE outlet_user_permissions (tenant_id uuid NOT NULL, outlet_id uuid NOT NULL, user_id uuid NOT NULL, revoked_at timestamptz NULL);
                CREATE TABLE "__EFMigrationsHistory" ("MigrationId" varchar(150) PRIMARY KEY, "ProductVersion" varchar(32) NOT NULL);
                """,
                connection))
            {
                await baseline.ExecuteNonQueryAsync();
            }

            await using (var seed = new NpgsqlCommand(
                """
                INSERT INTO tenants(id) VALUES (@tenant);
                INSERT INTO tenant_users(id, tenant_id) VALUES (@selectedUser, @tenant), (@tenantWideUser, @tenant);
                INSERT INTO outlet_user_roles(tenant_id, outlet_id, user_id, revoked_at) VALUES (@tenant, @outlet, @selectedUser, NULL);
                INSERT INTO tenant_user_roles(tenant_id, user_id, revoked_at) VALUES (@tenant, @tenantWideUser, NULL);
                """,
                connection))
            {
                seed.Parameters.AddWithValue("tenant", tenantId);
                seed.Parameters.AddWithValue("selectedUser", selectedUserId);
                seed.Parameters.AddWithValue("tenantWideUser", tenantWideUserId);
                seed.Parameters.AddWithValue("outlet", outletId);
                await seed.ExecuteNonQueryAsync();
            }

            await using var db = new EPosDbContext(
                new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);
            var script = db.Database.GetService<IMigrator>().GenerateScript(
                "20260824120000_SeedProductWizardSpecializedPermissions",
                "20260826120000_AddTenantUserExplicitOutletTillAccess");
            await using (var migrate = new NpgsqlCommand(script, connection))
            {
                await migrate.ExecuteNonQueryAsync();
            }

            await using var assertCommand = new NpgsqlCommand(
                """
                SELECT
                    (SELECT outlet_access_scope FROM tenant_users WHERE id = @selectedUser),
                    (SELECT outlet_access_scope FROM tenant_users WHERE id = @tenantWideUser),
                    to_regclass('public.tenant_user_till_access') IS NOT NULL,
                    EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'ck_tenant_users_till_access_scope');
                """,
                connection);
            assertCommand.Parameters.AddWithValue("selectedUser", selectedUserId);
            assertCommand.Parameters.AddWithValue("tenantWideUser", tenantWideUserId);
            await using var reader = await assertCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("SELECTED_OUTLETS", reader.GetString(0));
            Assert.Equal("ALL_OUTLETS", reader.GetString(1));
            Assert.True(reader.GetBoolean(2));
            Assert.True(reader.GetBoolean(3));
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

    private static string Describe(MigrationOperation operation) => operation switch
    {
        DropIndexOperation drop => $"DropIndex({drop.Table}.{drop.Name})",
        CreateIndexOperation create => $"CreateIndex({create.Table}.{create.Name})",
        _ => operation.GetType().Name,
    };
}
