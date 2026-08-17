using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.AccessControl;

public sealed class TenantAdminRoleRepositoryPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReplacePermissions_ReactivatesHistoricalGrant_InsteadOfInsertingDuplicateRow()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("tenant_role_permission_reactivation");
        var fixture = await SeedAsync(harness.ConnectionString);

        await using var db = CreateDb(harness.ConnectionString);
        var repository = new TenantAdminRoleRepository(db);

        await repository.ReplacePermissionsAsync(
            fixture.TenantId,
            fixture.AdminRoleId,
            [fixture.CriticalPermissionId],
            fixture.ActorUserId,
            Now.AddMinutes(5),
            CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var rows = await db.TenantRolePermissions
            .Where(permission =>
                permission.TenantId == fixture.TenantId &&
                permission.TenantRoleId == fixture.AdminRoleId &&
                permission.PermissionDefinitionId == fixture.CriticalPermissionId)
            .ToListAsync();

        var row = Assert.Single(rows);
        Assert.Null(row.RevokedAt);
        Assert.Null(row.RevokedByTenantUserId);
        Assert.Equal(fixture.ActorUserId, row.GrantedByTenantUserId);
    }

    [Fact]
    public async Task WouldReplaceAssignmentsRemoveLastAdminAsync_EvaluatesPostMutationAssignmentState()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("tenant_role_last_admin_assignment");
        var fixture = await SeedAsync(harness.ConnectionString);

        await using var db = CreateDb(harness.ConnectionString);
        var repository = new TenantAdminRoleRepository(db);
        await repository.ReplacePermissionsAsync(
            fixture.TenantId,
            fixture.AdminRoleId,
            [fixture.CriticalPermissionId],
            fixture.ActorUserId,
            Now.AddMinutes(5),
            CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var removingAllAssignments = await repository.WouldReplaceAssignmentsRemoveLastAdminAsync(
            fixture.TenantId,
            fixture.AdminRoleId,
            [],
            CancellationToken.None);

        var assigningAnotherActiveUser = await repository.WouldReplaceAssignmentsRemoveLastAdminAsync(
            fixture.TenantId,
            fixture.AdminRoleId,
            [new TenantAdminRoleAssignmentRequest(fixture.SecondUserId, "TENANT_WIDE")],
            CancellationToken.None);

        Assert.True(removingAllAssignments);
        Assert.False(assigningAnotherActiveUser);
    }

    private static async Task<FixtureIds> SeedAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();
        var moduleId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var revokedPermission = TenantRolePermission.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            fixture.AdminRoleId,
            fixture.CriticalPermissionId,
            fixture.ActorUserId,
            Now);
        revokedPermission.Revoke(fixture.ActorUserId, Now.AddMinutes(1));

        db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
        db.Tenants.Add(Tenant.Create(
            fixture.TenantId,
            $"TRR-{fixture.Suffix}",
            $"trr-{fixture.Suffix}",
            "Tenant Role Repository Test",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));
        db.PlatformModules.Add(PlatformModule.Create(moduleId, "tenant_admin", "Tenant Admin", null, "ACTIVE", 1, Now, true));
        db.PlatformFeatures.Add(PlatformFeature.Create(featureId, moduleId, "roles", "Roles", "ACTIVE", Now, 1, null, true));
        db.PermissionDefinitions.Add(PermissionDefinition.Create(
            fixture.CriticalPermissionId,
            TenantAdminUserPermissions.RolesManage,
            moduleId,
            featureId,
            "manage",
            "Manage roles",
            false,
            true,
            Now));
        db.TenantRoles.Add(TenantRole.Create(
            fixture.AdminRoleId,
            fixture.TenantId,
            null,
            null,
            $"ADMIN-{fixture.Suffix}",
            "Tenant Admin",
            null,
            true,
            true,
            fixture.ActorUserId,
            Now));
        db.TenantUsers.Add(TenantUser.Create(
            fixture.ActorUserId,
            fixture.TenantId,
            $"admin-{fixture.Suffix}@example.test",
            "Admin User",
            null,
            null,
            "hash",
            "salt",
            TenantUserConstants.StatusActive,
            "admin",
            "admin",
            null,
            Now,
            staffCode: $"USR-2026-A{fixture.Suffix[..4]}"));
        db.TenantUsers.Add(TenantUser.Create(
            fixture.SecondUserId,
            fixture.TenantId,
            $"second-{fixture.Suffix}@example.test",
            "Second User",
            null,
            null,
            "hash",
            "salt",
            TenantUserConstants.StatusActive,
            "admin",
            "admin",
            null,
            Now,
            staffCode: $"USR-2026-B{fixture.Suffix[..4]}"));
        db.TenantUserRoles.Add(TenantUserRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            fixture.ActorUserId,
            fixture.AdminRoleId,
            fixture.ActorUserId,
            Now));
        db.TenantRolePermissions.Add(revokedPermission);
        await db.SaveChangesAsync();
        return fixture;
    }

    private static EPosDbContext CreateDb(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(connectionString)
            .Options);

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

    private sealed class DisposablePostgresHarness : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _adminConnectionString;

        private DisposablePostgresHarness(string databaseName, string connectionString, string adminConnectionString)
        {
            _databaseName = databaseName;
            ConnectionString = connectionString;
            _adminConnectionString = adminConnectionString;
        }

        public string ConnectionString { get; }

        public static async Task<DisposablePostgresHarness> CreateAsync(string prefix)
        {
            var databaseName = $"{prefix}_{Guid.NewGuid():N}";
            var adminConnectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            await using (var admin = new NpgsqlConnection(adminConnectionString))
            {
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
                await create.ExecuteNonQueryAsync();
            }

            var connectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
            {
                Database = databaseName,
                IncludeErrorDetail = true
            }.ConnectionString;

            await using (var db = CreateDb(connectionString))
            {
                await db.Database.EnsureCreatedAsync();
                Assert.True(db.Database.IsNpgsql());
            }

            return new DisposablePostgresHarness(databaseName, connectionString, adminConnectionString);
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
                             admin))
            {
                terminate.Parameters.AddWithValue("database", _databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed record FixtureIds(
        Guid TenantId,
        Guid AdminRoleId,
        Guid ActorUserId,
        Guid SecondUserId,
        Guid CriticalPermissionId,
        string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }
}
