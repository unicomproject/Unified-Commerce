using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Shared.Idempotency.Services;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;
using E_POS.Infrastructure.Persistence;
using E_POS.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.AccessControl;

public sealed class TenantAdminUserCreateOptionsPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 8, 11, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCreateOptions_ReturnsTenantScopedSelectableLookupsAndEntitledPermissions()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedScopedOptionsAsync(harness.ConnectionString);
        await using var db = CreateDb(harness.ConnectionString);
        var service = CreateService(db);

        var result = await service.GetCreateOptionsAsync(
            new TenantRequestContext(
                fixture.TenantId,
                fixture.ActorUserId,
                [
                    TenantAdminUserPermissions.Create,
                    "tenant.users.update",
                    "platform.users.view"
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var options = result.Value!;
        var role = Assert.Single(options.Roles);
        Assert.Equal("Tenant Role", role.RoleName);
        var outlet = Assert.Single(options.Outlets);
        Assert.Equal("Tenant Outlet", outlet.OutletName);
        var till = Assert.Single(options.Tills!);
        Assert.Equal("Tenant Till", till.TillName);
        var group = Assert.Single(options.PermissionGroups);
        var permission = Assert.Single(group.Permissions);
        Assert.Equal("tenant.users.update", permission.PermissionCode);
        Assert.DoesNotContain(options.Roles, item => item.RoleName.Contains("Other", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(options.Roles, item => item.RoleCode.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(options.Outlets, item => item.OutletName.Contains("Other", StringComparison.OrdinalIgnoreCase));
        Assert.True(options.Capabilities!.SupportsExplicitTillAccess);
        Assert.False(options.Capabilities.SupportsPermissionDenies);
        Assert.False(string.IsNullOrWhiteSpace(options.PermissionCatalogVersion));
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsEmptyCollectionsWithoutThrowing()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedTenantOnlyAsync(harness.ConnectionString);
        await using var db = CreateDb(harness.ConnectionString);
        var service = CreateService(db);

        var result = await service.GetCreateOptionsAsync(
            new TenantRequestContext(fixture.TenantId, fixture.ActorUserId, [TenantAdminUserPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Roles);
        Assert.Empty(result.Value.Outlets);
        Assert.Empty(result.Value.PermissionGroups);
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsForbiddenWithoutRequiredPermission()
    {
        await using var db = CreateDbContextForPermissionOnly();
        var service = CreateService(db);

        var result = await service.GetCreateOptionsAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [TenantAdminUserPermissions.View]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
    }

    private static async Task<FixtureIds> SeedScopedOptionsAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();
        var otherTenantId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var userFeatureId = Guid.NewGuid();
        var reportsFeatureId = Guid.NewGuid();
        var tenantRoleId = Guid.NewGuid();
        var nonDelegableRoleId = Guid.NewGuid();
        var reportsPermissionId = Guid.NewGuid();
        var outletId = Guid.NewGuid();

        SeedCurrencyAndTenant(db, fixture.TenantId, fixture.Suffix);
        db.Tenants.Add(Tenant.Create(
            otherTenantId,
            $"OTHER-{fixture.Suffix}",
            $"other-{fixture.Suffix}",
            "Other Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));

        db.TenantRoles.AddRange(
            TenantRole.Create(tenantRoleId, fixture.TenantId, null, null, $"ROLE-A-{fixture.Suffix}", "Tenant Role", null, true, true, null, Now),
            TenantRole.Create(nonDelegableRoleId, fixture.TenantId, null, null, $"ROLE-NON-DELEGABLE-{fixture.Suffix}", "Nondelegable Role", null, true, true, null, Now),
            TenantRole.Create(Guid.NewGuid(), fixture.TenantId, null, null, "SUPER_ADMIN", "Tenant Super Admin", null, true, true, null, Now),
            TenantRole.Create(Guid.NewGuid(), fixture.TenantId, null, null, $"ROLE-INACTIVE-{fixture.Suffix}", "Inactive Role", null, true, false, null, Now),
            TenantRole.Create(Guid.NewGuid(), otherTenantId, null, null, $"ROLE-B-{fixture.Suffix}", "Other Tenant Role", null, true, true, null, Now));
        db.Outlets.AddRange(
            Outlet.Create(outletId, fixture.TenantId, "Tenant Outlet", $"OUT-A-{fixture.Suffix}", OutletConstants.ActiveStatus, OutletConstants.StoreOutletType, "UTC", true, null, null, null, Now),
            Outlet.Create(Guid.NewGuid(), fixture.TenantId, "Inactive Outlet", $"OUT-INACTIVE-{fixture.Suffix}", OutletConstants.InactiveStatus, OutletConstants.StoreOutletType, "UTC", false, null, null, null, Now),
            Outlet.Create(Guid.NewGuid(), otherTenantId, "Other Tenant Outlet", $"OUT-B-{fixture.Suffix}", OutletConstants.ActiveStatus, OutletConstants.StoreOutletType, "UTC", true, null, null, null, Now));
        db.Tills.Add(Till.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            outletId,
            "Tenant Till",
            "Front Counter",
            1,
            $"TILL-{fixture.Suffix}",
            TillConstants.StandardTillType,
            0m,
            "LKR",
            true,
            TillConstants.ActiveStatus,
            null,
            Now));
        db.PlatformModules.Add(PlatformModule.Create(moduleId, $"module-{fixture.Suffix}", "Tenant Admin", null, "ACTIVE", 1, Now));
        db.PlatformFeatures.AddRange(
            PlatformFeature.Create(userFeatureId, moduleId, PlatformTenantFeatureCodes.UserAccounts, "Users", "ACTIVE", Now),
            PlatformFeature.Create(reportsFeatureId, moduleId, PlatformTenantFeatureCodes.SalesReports, "Reports", "ACTIVE", Now));
        db.PermissionDefinitions.AddRange(
            PermissionDefinition.Create(Guid.NewGuid(), "tenant.users.update", moduleId, userFeatureId, "UPDATE", "Update users.", false, true, Now),
            PermissionDefinition.Create(reportsPermissionId, "tenant.reports.sales.view", moduleId, reportsFeatureId, "VIEW", "View reports.", false, true, Now),
            PermissionDefinition.Create(Guid.NewGuid(), "tenant.users.delete", moduleId, userFeatureId, "DELETE", "Inactive permission.", false, false, Now),
            PermissionDefinition.Create(Guid.NewGuid(), "platform.users.view", moduleId, userFeatureId, "VIEW", "Platform-only permission.", false, true, Now));
        db.TenantRolePermissions.Add(TenantRolePermission.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            nonDelegableRoleId,
            reportsPermissionId,
            null,
            Now));
        db.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            userFeatureId,
            TenantEntitlementStatusConstants.Enabled,
            Now));

        await db.SaveChangesAsync();
        return fixture;
    }

    private static async Task<FixtureIds> SeedTenantOnlyAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();
        SeedCurrencyAndTenant(db, fixture.TenantId, fixture.Suffix);
        await db.SaveChangesAsync();
        return fixture;
    }

    private static void SeedCurrencyAndTenant(EPosDbContext db, Guid tenantId, string suffix)
    {
        db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
        db.Tenants.Add(Tenant.Create(
            tenantId,
            $"OPT-{suffix}",
            $"opt-{suffix}",
            "Create Options Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));
    }

    private static TenantAdminUserService CreateService(EPosDbContext db) =>
        new(
            new IdempotencyService(db, new FixedDateTimeProvider(Now)),
            new TenantAdminUserRepository(db),
            new FixedDateTimeProvider(Now),
            new ThrowingPasswordHashService(),
            new PlatformPasswordPolicyValidator(),
            new AllowingTenantResourceLimitGuard(),
            new TenantUserStaffCodeService(db),
            new FakeInvitationTokenService(),
            new Lazy<IInvitationDeliverySecretProtector>(() => new FakeDeliverySecretProtector()));

    private static EPosDbContext CreateDb(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static EPosDbContext CreateDbContextForPermissionOnly() =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

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

        public static async Task<DisposablePostgresHarness> CreateAsync()
        {
            var databaseName = $"create_user_options_{Guid.NewGuid():N}";
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

    private sealed record FixtureIds(Guid TenantId, Guid ActorUserId, string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class ThrowingPasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => throw new InvalidOperationException("Password hashing is not used by create-options.");
        public bool VerifyPassword(string password, string passwordHash) => throw new InvalidOperationException("Password verification is not used by create-options.");
    }

    private sealed class FakeInvitationTokenService : IInvitationTokenService
    {
        public string GenerateToken() => "raw-token";
        public string HashToken(string rawToken) => "token-hash";
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) => new("cipher:" + rawToken, "test");
        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }
}
