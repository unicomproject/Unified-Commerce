using System.Reflection;
using E_POS.Application.Common.Contracts;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Services;
using E_POS.Infrastructure.Persistence;
using E_POS.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.AccessControl;

public sealed class TenantUserInviteDeliverySecretCleanupPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CleanupBatch_PurgesTerminalAndExpiredSecretsButRetainsPendingAndRetryable_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedFoundationAsync(harness.ConnectionString);

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var accepted = AddInviteSecret(db, fixture, "accepted@example.com", Now.AddDays(7));
            accepted.Invite.MarkAccepted(accepted.UserId, Now);
            AddInviteSecret(db, fixture, "revoked@example.com", Now.AddDays(7)).Invite.Revoke(Now);
            AddInviteSecret(db, fixture, "cancelled@example.com", Now.AddDays(7)).Invite.Cancel(Now);
            var expiredStatus = AddInviteSecret(db, fixture, "expired-status@example.com", Now.AddDays(7));
            SetInviteStatus(expiredStatus.Invite, UserInviteConstants.StatusExpired);
            AddInviteSecret(db, fixture, "expired-secret@example.com", Now.AddDays(-1));

            var terminal = AddInviteSecret(db, fixture, "terminal@example.com", Now.AddDays(7));
            var terminalOutbox = CreateInviteOutbox(terminal.UserId, fixture.TenantId, terminal.Invite.Id, Now);
            terminalOutbox.MarkFailed("terminal", "Terminal delivery failure.", terminal: true, Now, Now);
            db.IntegrationOutboxMessages.Add(terminalOutbox);

            AddInviteSecret(db, fixture, "pending@example.com", Now.AddDays(7));
            var retryable = AddInviteSecret(db, fixture, "retryable@example.com", Now.AddDays(7));
            var retryableOutbox = CreateInviteOutbox(retryable.UserId, fixture.TenantId, retryable.Invite.Id, Now);
            retryableOutbox.MarkFailed("retryable", "Retryable delivery failure.", terminal: false, Now.AddMinutes(10), Now);
            db.IntegrationOutboxMessages.Add(retryableOutbox);

            await db.SaveChangesAsync();
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var service = CreateCleanupService(db);
            var result = await service.CleanupBatchAsync(100, CancellationToken.None);

            Assert.Equal(6, result.PurgedCount);
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            Assert.Equal(6, await db.TenantUserInviteDeliverySecrets.CountAsync(x => x.PurgedAt != null));
            Assert.Equal(2, await db.TenantUserInviteDeliverySecrets.CountAsync(x => x.PurgedAt == null));
            Assert.All(
                await db.TenantUserInviteDeliverySecrets.Where(x => x.PurgedAt != null).ToListAsync(),
                secret => Assert.Equal(string.Empty, secret.EncryptedToken));
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var secondRun = await CreateCleanupService(db).CleanupBatchAsync(100, CancellationToken.None);
            Assert.Equal(0, secondRun.PurgedCount);
        }
    }

    [Fact]
    public async Task CleanupBatch_RespectsBatchLimitAndConcurrentRunsRemainIdempotent_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedFoundationAsync(harness.ConnectionString);

        await using (var db = CreateDb(harness.ConnectionString))
        {
            AddInviteSecret(db, fixture, "batch1@example.com", Now.AddDays(-1));
            AddInviteSecret(db, fixture, "batch2@example.com", Now.AddDays(-1));
            AddInviteSecret(db, fixture, "batch3@example.com", Now.AddDays(-1));
            await db.SaveChangesAsync();
        }

        await using (var firstDb = CreateDb(harness.ConnectionString))
        await using (var secondDb = CreateDb(harness.ConnectionString))
        {
            var results = await Task.WhenAll(
                CreateCleanupService(firstDb).CleanupBatchAsync(2, CancellationToken.None),
                CreateCleanupService(secondDb).CleanupBatchAsync(2, CancellationToken.None));

            Assert.InRange(results.Sum(result => result.PurgedCount), 2, 3);
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            await CreateCleanupService(db).CleanupBatchAsync(2, CancellationToken.None);
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            Assert.Equal(3, await db.TenantUserInviteDeliverySecrets.CountAsync(x => x.PurgedAt != null));
        }
    }

    private static TenantUserInviteDeliverySecretCleanupService CreateCleanupService(EPosDbContext db) =>
        new(db, new FixedDateTimeProvider(Now), NullLogger<TenantUserInviteDeliverySecretCleanupService>.Instance);

    private static InviteSecretSeed AddInviteSecret(
        EPosDbContext db,
        FixtureIds fixture,
        string email,
        DateTimeOffset secretExpiresAt)
    {
        var userId = Guid.NewGuid();
        var staffCodeSequence = db.ChangeTracker.Entries<TenantUser>().Count() + 1;
        var user = TenantUser.Create(
            userId,
            fixture.TenantId,
            email,
            email.Split('@')[0],
            null,
            null,
            TenantUserConstants.PendingInvitePasswordHash,
            "empty_salt",
            TenantUserConstants.StatusInvited,
            "admin",
            "admin",
            null,
            Now,
            staffCode: $"USR-{Now:yyyy}-{staffCodeSequence:00000}");
        var invite = UserInvite.CreatePending(
            Guid.NewGuid(),
            fixture.TenantId,
            email,
            TenantUser.NormalizeEmail(email),
            fixture.RoleId,
            null,
            $"hash-{Guid.NewGuid():N}",
            secretExpiresAt,
            Now,
            userId);
        var secret = TenantUserInviteDeliverySecret.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            userId,
            invite.Id,
            $"cipher-{Guid.NewGuid():N}",
            "test",
            secretExpiresAt,
            Now);

        db.TenantUsers.Add(user);
        db.UserInvites.Add(invite);
        db.TenantUserInviteDeliverySecrets.Add(secret);
        return new InviteSecretSeed(userId, invite, secret);
    }

    private static IntegrationOutboxMessage CreateInviteOutbox(
        Guid tenantUserId,
        Guid tenantId,
        Guid inviteId,
        DateTimeOffset now) =>
        IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            "tenant.user_invited",
            "TENANT_USER",
            tenantUserId,
            1,
            tenantId,
            Guid.NewGuid(),
            null,
            "{}",
            $"tenant.user_invited:{inviteId:N}",
            now);

    private static async Task<FixtureIds> SeedFoundationAsync(string connectionString)
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
            $"ICS-{fixture.Suffix}",
            $"ics-{fixture.Suffix}",
            "Invite Cleanup Tenant",
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
            "Invite Cleanup Role",
            "Role for invite cleanup tests.",
            true,
            true,
            null,
            Now));

        await db.SaveChangesAsync();
        return fixture;
    }

    private static void SetInviteStatus(UserInvite invite, string status)
    {
        typeof(UserInvite)
            .GetProperty(nameof(UserInvite.InviteStatus), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(invite, status);
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
            var databaseName = $"invite_secret_cleanup_{Guid.NewGuid():N}";
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

    private sealed record FixtureIds(Guid TenantId, Guid RoleId, string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new FixtureIds(Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }

    private sealed record InviteSecretSeed(Guid UserId, UserInvite Invite, TenantUserInviteDeliverySecret Secret);

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
