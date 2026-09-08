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
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
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

public sealed class TenantAdminCreateUserIdempotencyPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConcurrentInvitedCreate_SameKey_CreatesOneUserInviteSecretAndOutbox_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedAsync(harness.ConnectionString);

        await using var firstDb = CreateDb(harness.ConnectionString);
        await using var secondDb = CreateDb(harness.ConnectionString);
        var firstService = CreateService(firstDb);
        var secondService = CreateService(secondDb);
        var request = CreateRequest(fixture.RoleId);
        var context = new TenantRequestContext(
            fixture.TenantId,
            fixture.ActorUserId,
            [TenantAdminUserPermissions.Invite]);
        var barrier = new Barrier(2);
        var results = new ApplicationResult<TenantAdminUserDetailResponse>?[2];

        async Task RunCreateAsync(int index, TenantAdminUserService service)
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(30));
            results[index] = await service.CreateAsync(
                context,
                request,
                CancellationToken.None,
                "postgres-create-user-key");
        }

        await Task.WhenAll(
            RunCreateAsync(0, firstService),
            RunCreateAsync(1, secondService));

        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);
        Assert.True(results[0]!.IsSuccess);
        Assert.True(results[1]!.IsSuccess);
        Assert.Equal(results[0]!.Value!.UserId, results[1]!.Value!.UserId);
        Assert.Equal(results[0]!.Value!.StaffCode, results[1]!.Value!.StaffCode);

        await using var assertDb = CreateDb(harness.ConnectionString);
        Assert.Equal(1, await assertDb.TenantUsers.CountAsync(x => x.TenantId == fixture.TenantId));
        Assert.Equal(1, await assertDb.UserInvites.CountAsync(x => x.TenantId == fixture.TenantId));
        Assert.Equal(1, await assertDb.TenantUserInviteDeliverySecrets.CountAsync(x => x.TenantId == fixture.TenantId));
        Assert.Equal(1, await assertDb.IntegrationOutboxMessages.CountAsync(x =>
            x.TenantId == fixture.TenantId &&
            x.MessageType == "tenant.user_invited"));
        Assert.Equal(1, await assertDb.IdempotencyRequests.CountAsync(x =>
            x.TenantId == fixture.TenantId &&
            x.ActorUserId == fixture.ActorUserId &&
            x.Endpoint == "TENANT_ADMIN_CREATE_USER" &&
            x.IdempotencyKey == "postgres-create-user-key" &&
            x.Status == "COMPLETED"));
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

    private static TenantAdminUserCreateRequest CreateRequest(Guid roleId) =>
        new(
            "Postgres Invite",
            "postgres.invite@example.com",
            "+94770000000",
            roleId,
            [],
            false,
            [],
            true,
            AccountStatus: TenantUserConstants.StatusInvited);

    private static async Task<FixtureIds> SeedAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();

        db.Currencies.Add(Currency.Create(
            Guid.NewGuid(),
            "LKR",
            "Sri Lankan Rupee",
            "Rs",
            2,
            true,
            1,
            Now));
        db.Tenants.Add(Tenant.Create(
            fixture.TenantId,
            $"ICU-{fixture.Suffix}",
            $"icu-{fixture.Suffix}",
            "Idempotent Create User Tenant",
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
            "Invite Role",
            "Role without permissions for idempotency proof.",
            true,
            true,
            null,
            Now));

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

        public static async Task<DisposablePostgresHarness> CreateAsync()
        {
            var databaseName = $"create_user_idempotency_{Guid.NewGuid():N}";
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
        public string GenerateToken() => "postgres-raw-token";
        public string HashToken(string rawToken) => "postgres-token-hash";
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) => new("cipher:" + rawToken, "test");
        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }
}
