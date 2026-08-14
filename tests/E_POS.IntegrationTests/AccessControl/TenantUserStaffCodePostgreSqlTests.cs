using System.Text.RegularExpressions;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Shared.Idempotency.Services;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;
using E_POS.Infrastructure.Persistence;
using E_POS.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.AccessControl;

public sealed class TenantUserStaffCodePostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly Regex StaffCodeRegex = new("^USR-2026-[0-9]{5}$", RegexOptions.Compiled);

    [Fact]
    public async Task CreateUser_ConcurrentSameTenant_GeneratesUniqueStaffCodes()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("staff_code_same_tenant");
        var fixture = await SeedTenantAsync(harness.ConnectionString);

        var tasks = Enumerable.Range(0, 8)
            .Select(async index =>
            {
                await using var db = CreateDb(harness.ConnectionString);
                var service = CreateUserService(db);
                var context = new TenantRequestContext(
                    fixture.TenantId,
                    fixture.ActorUserId,
                    [TenantAdminUserPermissions.Create]);

                return await service.CreateAsync(
                    context,
                    CreateRequest($"same-{index}@example.test", fixture.RoleId),
                    CancellationToken.None,
                    $"same-key-{index}");
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        var staffCodes = results.Select(result => result.Value!.StaffCode).ToArray();
        Assert.All(staffCodes, staffCode =>
        {
            Assert.False(string.IsNullOrWhiteSpace(staffCode));
            Assert.Matches(StaffCodeRegex, staffCode!);
        });
        Assert.Equal(staffCodes.Length, staffCodes.Distinct(StringComparer.Ordinal).Count());

        await using var assertDb = CreateDb(harness.ConnectionString);
        Assert.Equal(8, await assertDb.TenantUsers.CountAsync(user => user.TenantId == fixture.TenantId));
    }

    [Fact]
    public async Task StaffCodeService_ConcurrentDifferentTenants_UsesTenantScopedSequences()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("staff_code_multi_tenant");
        await using var setupDb = CreateDb(harness.ConnectionString);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var tasks = new[]
        {
            GenerateAsync(harness.ConnectionString, tenantA),
            GenerateAsync(harness.ConnectionString, tenantA),
            GenerateAsync(harness.ConnectionString, tenantB),
            GenerateAsync(harness.ConnectionString, tenantB),
        };

        var codes = await Task.WhenAll(tasks);

        Assert.Equal(2, codes.Take(2).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, codes.Skip(2).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("USR-2026-00001", codes.Take(2));
        Assert.Contains("USR-2026-00001", codes.Skip(2));
        Assert.All(codes, code => Assert.Matches(StaffCodeRegex, code));
    }

    [Fact]
    public async Task CreateTenantWizard_AssignsStaffCodeToBootstrapTenantAdmin()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("staff_code_bootstrap");
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = TenantUser.CreatePendingInvite(
            Guid.NewGuid(),
            tenantId,
            "bootstrap-admin@example.test",
            "Bootstrap Admin",
            null,
            null,
            Now);

        await using (var db = CreateDb(harness.ConnectionString))
        {
            db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
            db.SubscriptionPlans.Add(SubscriptionPlan.Create(
                planId,
                "BOOTSTRAP",
                "Bootstrap Plan",
                SubscriptionPlanConstants.Status.Active,
                SubscriptionPlanConstants.BillingInterval.Monthly,
                1000m,
                Now));
            await db.SaveChangesAsync();

            var repository = new PlatformTenantRepository(db);
            await repository.CreateTenantWizardAsync(new PlatformTenantCreateWriteModel
            {
                Tenant = Tenant.Create(
                    tenantId,
                    "BOOT-001",
                    "boot-001",
                    "Bootstrap Tenant",
                    TenantStatusConstants.Active,
                    "LKR",
                    "UTC",
                    null,
                    null,
                    Now),
                Subscription = TenantSubscription.Create(
                    Guid.NewGuid(),
                    tenantId,
                    planId,
                    TenantSubscriptionStatusConstants.Active,
                    Now),
                TenantAdminRole = TenantRole.Create(
                    roleId,
                    tenantId,
                    null,
                    null,
                    "TENANT-ADMIN",
                    "Tenant Admin",
                    null,
                    true,
                    true,
                    null,
                    Now),
                TenantAdminUser = user,
                TenantAdminUserRole = TenantUserRole.Create(Guid.NewGuid(), tenantId, user.Id, roleId, null, Now),
            }, CancellationToken.None);
        }

        await using var assertDb = CreateDb(harness.ConnectionString);
        var savedUser = await assertDb.TenantUsers.SingleAsync(dbUser => dbUser.Id == user.Id);
        Assert.Equal("USR-2026-00001", savedUser.StaffCode);
    }

    [Fact]
    public void TenantUser_AssignStaffCode_DoesNotAllowChangingExistingCode()
    {
        var user = TenantUser.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "immutable@example.test",
            "Immutable User",
            null,
            null,
            "hash",
            "salt",
            TenantUserConstants.StatusInactive,
            "standard",
            "system",
            null,
            Now,
            staffCode: "USR-2026-00001");

        var error = Assert.Throws<InvalidOperationException>(() =>
            user.AssignStaffCode("USR-2026-00002", Now.AddMinutes(1)));

        Assert.Equal("Staff code is immutable.", error.Message);
        Assert.Equal("USR-2026-00001", user.StaffCode);
    }

    [Fact]
    public async Task CompleteStaffCodeRolloutMigration_BackfillsAndSyncsSequence()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync("staff_code_migration", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        await CreateMinimalLegacySchemaAsync(db);

        var tenantId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tenant_users (id, tenant_id, email, full_name, encrypted_password, password_salt, account_status, user_type, source_user_type, created_at, updated_at, staff_code)
            VALUES
                ({Guid.NewGuid()}, {tenantId}, 'valid@example.test', 'Valid User', 'hash', 'salt', 'ACTIVE', 'standard', 'system', {Now.AddMinutes(-3)}, {Now.AddMinutes(-3)}, 'USR-2026-00027'),
                ({Guid.NewGuid()}, {tenantId}, 'missing@example.test', 'Missing User', 'hash', 'salt', 'ACTIVE', 'standard', 'system', {Now.AddMinutes(-2)}, {Now.AddMinutes(-2)}, NULL),
                ({Guid.NewGuid()}, {tenantId}, 'invalid@example.test', 'Invalid User', 'hash', 'salt', 'ACTIVE', 'standard', 'system', {Now.AddMinutes(-1)}, {Now.AddMinutes(-1)}, 'legacy-01');
            """);

        var migrator = db.Database.GetService<IMigrator>();
        var script = migrator.GenerateScript(
            "20260810120500_EnsureTargetPosLoginBrandingProfile",
            "20260810143000_CompleteTenantUserStaffCodeRollout");
        await using (var connection = new NpgsqlConnection(harness.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();
        }

        var staffCodes = await db.TenantUsers
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId)
            .OrderBy(user => user.StaffCode)
            .Select(user => user.StaffCode!)
            .ToListAsync();

        Assert.Contains("USR-2026-00027", staffCodes);
        Assert.Contains("USR-2026-00028", staffCodes);
        Assert.Contains("USR-2026-00029", staffCodes);

        var nextCode = await new TenantUserStaffCodeService(db).GenerateAsync(tenantId, Now, CancellationToken.None);
        Assert.Equal("USR-2026-00030", nextCode);

        var nullable = await db.Database.SqlQueryRaw<string>(
                """
                SELECT is_nullable AS "Value"
                FROM information_schema.columns
                WHERE table_name = 'tenant_users' AND column_name = 'staff_code'
                """)
            .SingleAsync();
        Assert.Equal("NO", nullable);

        var uniqueIndexCount = await db.Database.SqlQueryRaw<int>(
                """
                SELECT count(*)::int AS "Value"
                FROM pg_indexes
                WHERE tablename = 'tenant_users'
                  AND indexname = 'uq_tenant_users_tenant_id_staff_code'
                  AND indexdef LIKE '%tenant_id, staff_code%'
                """)
            .SingleAsync();
        Assert.Equal(1, uniqueIndexCount);
    }

    private static async Task<string> GenerateAsync(string connectionString, Guid tenantId)
    {
        await using var db = CreateDb(connectionString);
        return await new TenantUserStaffCodeService(db).GenerateAsync(tenantId, Now, CancellationToken.None);
    }

    private static TenantAdminUserService CreateUserService(EPosDbContext db) =>
        new(
            new IdempotencyService(db, new FixedDateTimeProvider(Now)),
            new TenantAdminUserRepository(db),
            new FixedDateTimeProvider(Now),
            new ThrowingPasswordHashService(),
            new AllowingTenantResourceLimitGuard(),
            new TenantUserStaffCodeService(db),
            new FakeInvitationTokenService(),
            new Lazy<IInvitationDeliverySecretProtector>(() => new FakeDeliverySecretProtector()));

    private static TenantAdminUserCreateRequest CreateRequest(string email, Guid roleId) =>
        new(
            "Staff Code User",
            email,
            null,
            roleId,
            [],
            false,
            [],
            false,
            AccountStatus: TenantUserConstants.StatusInactive);

    private static async Task<FixtureIds> SeedTenantAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();
        db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
        db.Tenants.Add(Tenant.Create(
            fixture.TenantId,
            $"SC-{fixture.Suffix}",
            $"sc-{fixture.Suffix}",
            "Staff Code Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));
        db.TenantRoles.Add(TenantRole.Create(
            fixture.RoleId,
            fixture.TenantId,
            null,
            null,
            $"ROLE-{fixture.Suffix}",
            "Staff Code Role",
            "Role for staff code tests.",
            true,
            true,
            null,
            Now));
        await db.SaveChangesAsync();
        return fixture;
    }

    private static async Task CreateMinimalLegacySchemaAsync(EPosDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE tenant_users (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                email varchar(320) NOT NULL,
                full_name varchar(200) NOT NULL,
                encrypted_password text NOT NULL,
                password_salt text NOT NULL,
                account_status varchar(20) NOT NULL,
                user_type varchar(50) NOT NULL,
                source_user_type varchar(50) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                staff_code varchar(20) NULL
            );

            CREATE UNIQUE INDEX uq_tenant_users_tenant_id_staff_code
                ON tenant_users (tenant_id, staff_code)
                WHERE staff_code IS NOT NULL;

            CREATE TABLE tenant_user_code_sequences (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                sequence_type varchar(64) NOT NULL,
                year integer NOT NULL,
                current_value bigint NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX uq_tenant_user_code_sequences_scope
                ON tenant_user_code_sequences (tenant_id, sequence_type, year);

            CREATE TABLE tenant_user_invite_delivery_secrets (
                id uuid PRIMARY KEY,
                purged_at timestamp with time zone NULL,
                expires_at timestamp with time zone NOT NULL,
                created_at timestamp with time zone NOT NULL
            );

            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES
                ('20260810120000_AddTenantUserInviteSecurityFoundation', '10.0.0'),
                ('20260810120000_RemoveTestMerchandiseProductsFromDatabase', '10.0.0'),
                ('20260810120000_SeedPosLoginBrandingSettingDefinitions', '10.0.0'),
                ('20260810120100_NormalizePosLoginOptionalMediaDefaults', '10.0.0'),
                ('20260810120200_SeedPosLoginBrandingDevelopmentFixtures', '10.0.0'),
                ('20260810120300_CorrectPosLoginBrandingSubtitleTemplate', '10.0.0'),
                ('20260810120400_SeedTargetPosLoginBrandingAssets', '10.0.0'),
                ('20260810120500_EnsureTargetPosLoginBrandingProfile', '10.0.0');
            """);
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

        public static async Task<DisposablePostgresHarness> CreateAsync(string prefix, bool ensureCreated = true)
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

            if (ensureCreated)
            {
                await using var db = CreateDb(connectionString);
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

    private sealed record FixtureIds(Guid TenantId, Guid RoleId, Guid ActorUserId, string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class ThrowingPasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => throw new InvalidOperationException("Password hashing is not used by create-user.");
        public bool VerifyPassword(string password, string passwordHash) => throw new InvalidOperationException("Password verification is not used by create-user.");
    }

    private sealed class FakeInvitationTokenService : IInvitationTokenService
    {
        public string GenerateToken() => "staff-code-raw-token";
        public string HashToken(string rawToken) => "staff-code-token-hash";
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) => new("cipher:" + rawToken, "test");
        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }
}
