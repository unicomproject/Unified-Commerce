using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Shared.Idempotency.Services;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.AccessControl;

public sealed class TenantAdminInviteResendRevokePostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddMinutes(-5);

    [Fact]
    public async Task ResendRevoke_WorkerAndAcceptance_UseExactCurrentInviteOnly_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedAsync(harness.ConnectionString);
        var tokens = new RecordingInvitationTokenService();
        var protector = new FakeDeliverySecretProtector();

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var service = CreateUserService(db, tokens, protector);
            var context = new TenantRequestContext(fixture.TenantId, fixture.ActorUserId, [TenantAdminUserPermissions.Invite]);

            var userA = await service.CreateAsync(context, CreateRequest("worker-a@example.test", fixture.RoleId), CancellationToken.None, "create-a");
            var userB = await service.CreateAsync(context, CreateRequest("worker-b@example.test", fixture.RoleId), CancellationToken.None, "create-b");
            Assert.True(userA.IsSuccess);
            Assert.True(userB.IsSuccess);

            var resendA = await service.ResendInviteAsync(context, userA.Value!.UserId, CancellationToken.None);
            var revokeB = await service.RevokeInviteAsync(context, userB.Value!.UserId, CancellationToken.None);
            Assert.True(resendA.IsSuccess);
            Assert.True(revokeB.IsSuccess);
        }

        var tokenA1 = tokens.GeneratedTokens[0];
        var tokenB1 = tokens.GeneratedTokens[1];
        var tokenA2 = tokens.GeneratedTokens[2];

        await using (var assertDb = CreateDb(harness.ConnectionString))
        {
            var invites = await assertDb.UserInvites
                .AsNoTracking()
                .Where(invite => invite.TenantId == fixture.TenantId)
                .ToListAsync();
            Assert.Equal(3, invites.Count);
            Assert.Equal(UserInviteConstants.StatusRevoked, invites.Single(invite => invite.InviteTokenHash == tokens.HashFor(tokenA1)).InviteStatus);
            Assert.Equal(UserInviteConstants.StatusRevoked, invites.Single(invite => invite.InviteTokenHash == tokens.HashFor(tokenB1)).InviteStatus);
            Assert.Equal(UserInviteConstants.StatusPending, invites.Single(invite => invite.InviteTokenHash == tokens.HashFor(tokenA2)).InviteStatus);
            Assert.Equal(3, await assertDb.IntegrationOutboxMessages.CountAsync(message =>
                message.TenantId == fixture.TenantId &&
                message.MessageType == "tenant.user_invited"));
            Assert.Contains(await assertDb.AuditLogs.AsNoTracking().Where(audit => audit.TenantId == fixture.TenantId).ToListAsync(),
                audit => audit.Action == "user.invite_resent");
            Assert.Contains(await assertDb.AuditLogs.AsNoTracking().Where(audit => audit.TenantId == fixture.TenantId).ToListAsync(),
                audit => audit.Action == "user.invite_revoked");
        }

        var sender = new RecordingEmailSender();
        var worker = CreateWorker(harness.ConnectionString, sender, protector);
        await worker.RunSingleBatchAsync();

        Assert.Single(sender.SentMessages);
        Assert.Equal("worker-a@example.test", sender.SentMessages[0].ToAddress, ignoreCase: true);

        var acceptance = CreateAcceptanceService(harness.ConnectionString, tokens);
        var oldA = await acceptance.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest(tokenA1, "Password1", "Password1"),
            CancellationToken.None);
        var revokedB = await acceptance.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest(tokenB1, "Password1", "Password1"),
            CancellationToken.None);
        var newA = await acceptance.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest(tokenA2, "Password1", "Password1"),
            CancellationToken.None);
        var replayA = await acceptance.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest(tokenA2, "Password1", "Password1"),
            CancellationToken.None);

        Assert.True(oldA.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteCancelled, oldA.Error.Code);
        Assert.True(revokedB.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteCancelled, revokedB.Error.Code);
        Assert.True(newA.IsSuccess);
        Assert.True(replayA.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteUsed, replayA.Error.Code);
    }

    private static TenantAdminUserService CreateUserService(
        EPosDbContext db,
        RecordingInvitationTokenService tokens,
        IInvitationDeliverySecretProtector protector) =>
        new(
            new IdempotencyService(db, new FixedDateTimeProvider(Now)),
            new TenantAdminUserRepository(db),
            new FixedDateTimeProvider(Now),
            new PasswordHashService(),
            new AllowingTenantResourceLimitGuard(),
            new TenantUserStaffCodeService(db),
            tokens,
            new Lazy<IInvitationDeliverySecretProtector>(() => protector));

    private static TenantAdminInvitationAcceptanceService CreateAcceptanceService(
        string connectionString,
        RecordingInvitationTokenService tokens)
    {
        var db = CreateDb(connectionString);
        return new TenantAdminInvitationAcceptanceService(
            new TenantAdminInvitationAcceptanceRepository(db),
            tokens,
            new PasswordHashService(),
            new PlatformPasswordPolicyValidator(),
            new FixedDateTimeProvider(Now),
            NullLogger<TenantAdminInvitationAcceptanceService>.Instance);
    }

    private static TestWorkerWrapper CreateWorker(
        string connectionString,
        RecordingEmailSender sender,
        IInvitationDeliverySecretProtector protector)
    {
        var services = new ServiceCollection();
        services.AddDbContext<EPosDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IApplicationEmailSender>(sender);
        services.AddSingleton(protector);
        var provider = services.BuildServiceProvider();
        var worker = new TenantOnboardingOutboxWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TenantOnboardingOutboxOptions
            {
                Enabled = true,
                BatchSize = 10,
                TenantAdminAppBaseUrl = "http://localhost:4200",
            }),
            NullLogger<TenantOnboardingOutboxWorker>.Instance);
        return new TestWorkerWrapper(worker, provider);
    }

    private static TenantAdminUserCreateRequest CreateRequest(string email, Guid roleId) =>
        new("Invite User", email, null, roleId, [], false, [], true, AccountStatus: TenantUserConstants.StatusInvited);

    private static async Task<FixtureIds> SeedAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();
        db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
        db.Tenants.Add(Tenant.Create(
            fixture.TenantId,
            $"IRR-{fixture.Suffix}",
            $"irr-{fixture.Suffix}",
            "Invite Resend Revoke Tenant",
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
            "Role without permissions for invite mutation proof.",
            true,
            true,
            null,
            Now));
        await db.SaveChangesAsync();
        return fixture;
    }

    private static EPosDbContext CreateDb(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

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
            var databaseName = $"invite_resend_revoke_{Guid.NewGuid():N}";
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

    private sealed class RecordingInvitationTokenService : IInvitationTokenService
    {
        private readonly Dictionary<string, string> _hashes = [];
        private int _nextToken = 1;

        public List<string> GeneratedTokens { get; } = [];

        public string GenerateToken()
        {
            var rawToken = $"raw-invite-token-{_nextToken}";
            _hashes[rawToken] = $"invite-token-hash-{_nextToken}";
            _nextToken++;
            GeneratedTokens.Add(rawToken);
            return rawToken;
        }

        public string HashToken(string rawToken) => _hashes.TryGetValue(rawToken, out var hash)
            ? hash
            : throw new InvalidOperationException("Unknown token.");

        public string HashFor(string rawToken) => HashToken(rawToken);
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) => new("cipher:" + rawToken, "test");
        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }

    private sealed class RecordingEmailSender : IApplicationEmailSender
    {
        public bool IsConfigured => true;
        public List<ApplicationEmailMessage> SentMessages { get; } = [];

        public Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(
            ApplicationEmailMessage message,
            CancellationToken cancellationToken)
        {
            SentMessages.Add(message);
            return Task.FromResult(ApplicationResult<ApplicationEmailSendResult>.Success(
                new ApplicationEmailSendResult("email-op", "Started", "email-op")));
        }
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class TestWorkerWrapper : IAsyncDisposable
    {
        private readonly TenantOnboardingOutboxWorker _worker;
        private readonly ServiceProvider _provider;

        public TestWorkerWrapper(TenantOnboardingOutboxWorker worker, ServiceProvider provider)
        {
            _worker = worker;
            _provider = provider;
        }

        public async Task RunSingleBatchAsync()
        {
            var claimMethod = typeof(TenantOnboardingOutboxWorker).GetMethod("ClaimAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var processMethod = typeof(TenantOnboardingOutboxWorker).GetMethod("ProcessAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var claimedIds = await (Task<IReadOnlyList<Guid>>)claimMethod!.Invoke(_worker, [CancellationToken.None])!;
            foreach (var id in claimedIds)
            {
                await (Task)processMethod!.Invoke(_worker, [id, CancellationToken.None])!;
            }
        }

        public ValueTask DisposeAsync() => _provider.DisposeAsync();
    }
}
