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

    [Fact]
    public async Task WouldRemoveLastAdminAsync_IgnoresInactiveAdministrativePermissionDefinitions()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("tenant_role_inactive_admin_permission");
        var fixture = await SeedAsync(harness.ConnectionString, criticalPermissionIsActive: false);

        await using var db = CreateDb(harness.ConnectionString);
        var repository = new TenantAdminRoleRepository(db);

        var wouldRemoveLastAdmin = await repository.WouldRemoveLastAdminAsync(
            fixture.TenantId,
            fixture.AdminRoleId,
            null,
            false,
            CancellationToken.None);

        Assert.False(wouldRemoveLastAdmin);
    }

    [Fact]
    public async Task GetSetupRoleOptionsAsync_ReturnsOnlyCanonicalTenantAdminAndCashierRoles()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("tenant_role_setup_options");
        var fixture = await SeedAsync(harness.ConnectionString);

        await using var db = CreateDb(harness.ConnectionString);
        db.TenantRoles.Add(TenantRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            null,
            null,
            TenantUserConstants.DefaultCashierRoleCode,
            "Cashier",
            null,
            false,
            true,
            fixture.ActorUserId,
            Now));
        db.TenantRoles.Add(TenantRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            null,
            null,
            "SUPER_ADMIN",
            "Super Admin",
            null,
            false,
            true,
            fixture.ActorUserId,
            Now));
        await db.SaveChangesAsync();

        var repository = new TenantAdminRoleRepository(db);
        var result = await repository.GetSetupRoleOptionsAsync(fixture.TenantId, CancellationToken.None);

        Assert.Collection(
            result.OrderBy(role => role.RoleCode, StringComparer.Ordinal),
            role => Assert.Equal(TenantUserConstants.DefaultCashierRoleCode, role.RoleCode),
            role => Assert.Equal(TenantUserConstants.DefaultTenantAdminRoleCode, role.RoleCode));
    }

    [Fact]
    public async Task GetPermissionCatalogAsync_IncludesActiveSystemPermissionWhenActorCanDelegateIt()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("tenant_role_system_catalog");
        var fixture = await SeedAsync(harness.ConnectionString);

        await using var db = CreateDb(harness.ConnectionString);
        var moduleId = await db.PlatformModules.Select(module => module.Id).SingleAsync();
        var featureId = await db.PlatformFeatures.Select(feature => feature.Id).SingleAsync();
        const string permissionCode = "pos.sales.checkout";

        db.PermissionDefinitions.Add(PermissionDefinition.Create(
            Guid.NewGuid(),
            permissionCode,
            moduleId,
            featureId,
            "checkout",
            "Checkout a POS sale.",
            true,
            true,
            Now));
        await db.SaveChangesAsync();

        var repository = new TenantAdminRoleRepository(db);
        var catalog = await repository.GetPermissionCatalogAsync(
            fixture.TenantId,
            [permissionCode],
            Now,
            CancellationToken.None);

        Assert.Contains(
            catalog.Modules.SelectMany(module => module.Features)
                .SelectMany(feature => feature.Permissions),
            permission => permission.Code == permissionCode && permission.Assignable);
    }

    [Fact]
    public async Task GetAssignablePermissionsByCodeAsync_AcceptsActiveSystemPermissionWhenActorCanDelegateIt()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("tenant_role_system_assignable");
        var fixture = await SeedAsync(harness.ConnectionString);

        await using var db = CreateDb(harness.ConnectionString);
        var moduleId = await db.PlatformModules.Select(module => module.Id).SingleAsync();
        var featureId = await db.PlatformFeatures.Select(feature => feature.Id).SingleAsync();
        const string permissionCode = "pos.sales.checkout";

        db.PermissionDefinitions.Add(PermissionDefinition.Create(
            Guid.NewGuid(),
            permissionCode,
            moduleId,
            featureId,
            "checkout",
            "Checkout a POS sale.",
            true,
            true,
            Now));
        await db.SaveChangesAsync();

        var repository = new TenantAdminRoleRepository(db);
        var assignable = await repository.GetAssignablePermissionsByCodeAsync(
            fixture.TenantId,
            [permissionCode],
            [permissionCode],
            Now,
            CancellationToken.None);

        var permission = Assert.Single(assignable);
        Assert.Equal(permissionCode, permission.PermissionCode);
    }

    private static async Task<FixtureIds> SeedAsync(string connectionString, bool criticalPermissionIsActive = true)
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
            criticalPermissionIsActive,
            Now));
        db.TenantRoles.Add(TenantRole.Create(
            fixture.AdminRoleId,
            fixture.TenantId,
            null,
            null,
            TenantUserConstants.DefaultTenantAdminRoleCode,
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
