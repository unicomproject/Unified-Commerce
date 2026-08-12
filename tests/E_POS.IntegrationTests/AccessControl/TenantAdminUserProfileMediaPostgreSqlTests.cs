using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
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

public sealed class TenantAdminUserProfileMediaPostgreSqlTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateUpdateAndRemove_ProfileMedia_UsesMediaAssetLifecycleAndSafeAudits_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = await SeedFoundationAsync(harness.ConnectionString);
        var firstMediaAssetId = Guid.NewGuid();
        var secondMediaAssetId = Guid.NewGuid();
        var wrongTenantMediaAssetId = Guid.NewGuid();
        var expiredMediaAssetId = Guid.NewGuid();

        await using (var db = CreateDb(harness.ConnectionString))
        {
            db.MediaAssets.Add(CreateImageAsset(firstMediaAssetId, fixture.TenantId, "profile-1.jpg", "https://cdn.example.test/profile-1.jpg"));
            db.MediaAssets.Add(CreateImageAsset(secondMediaAssetId, fixture.TenantId, "profile-2.jpg", "https://cdn.example.test/profile-2.jpg"));
            db.MediaAssets.Add(CreateImageAsset(wrongTenantMediaAssetId, fixture.OtherTenantId, "wrong.jpg", "https://cdn.example.test/wrong.jpg"));
            var expired = CreateImageAsset(expiredMediaAssetId, fixture.TenantId, "expired.jpg", "https://cdn.example.test/expired.jpg");
            expired.MarkDeletePending(fixture.ActorUserId, Now);
            db.MediaAssets.Add(expired);
            await db.SaveChangesAsync();
        }

        TenantAdminUserDetailResponse createResponse;
        await using (var db = CreateDb(harness.ConnectionString))
        {
            var service = CreateService(db);
            var createResult = await service.CreateAsync(
                CreateContext(fixture, [TenantAdminUserPermissions.Create]),
                CreateRequest(fixture.RoleId, firstMediaAssetId),
                CancellationToken.None,
                "profile-media-create");

            Assert.True(createResult.IsSuccess);
            createResponse = createResult.Value!;
            Assert.Equal("https://cdn.example.test/profile-1.jpg", createResponse.ProfileImageUrl);
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var user = await db.TenantUsers.SingleAsync(x => x.Id == createResponse.UserId);
            Assert.Equal(firstMediaAssetId, user.ProfileImageUrl);
            Assert.Contains(await db.AuditLogs.ToListAsync(), audit =>
                audit.Action == "user.profile_image_assigned" &&
                audit.NewValues != null &&
                audit.NewValues.Contains(firstMediaAssetId.ToString()) &&
                !audit.NewValues.Contains("profile-1.jpg"));
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var service = CreateService(db);
            var replaceResult = await service.UpdateAsync(
                CreateContext(fixture, [TenantAdminUserPermissions.Update]),
                createResponse.UserId,
                UpdateRequest(fixture.RoleId) with { ProfileMediaAssetId = secondMediaAssetId },
                CancellationToken.None);

            Assert.True(replaceResult.IsSuccess);
            Assert.Equal("https://cdn.example.test/profile-2.jpg", replaceResult.Value!.ProfileImageUrl);
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var user = await db.TenantUsers.SingleAsync(x => x.Id == createResponse.UserId);
            var oldAsset = await db.MediaAssets.SingleAsync(x => x.Id == firstMediaAssetId);
            Assert.Equal(secondMediaAssetId, user.ProfileImageUrl);
            Assert.Equal("INACTIVE", oldAsset.Status);
            Assert.Contains(await db.AuditLogs.ToListAsync(), audit => audit.Action == "user.profile_image_replaced");
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var service = CreateService(db);
            var removeResult = await service.UpdateAsync(
                CreateContext(fixture, [TenantAdminUserPermissions.Update]),
                createResponse.UserId,
                UpdateRequest(fixture.RoleId) with { ProfileMediaAction = "REMOVE" },
                CancellationToken.None);

            Assert.True(removeResult.IsSuccess);
            Assert.Null(removeResult.Value!.ProfileImageUrl);
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var user = await db.TenantUsers.SingleAsync(x => x.Id == createResponse.UserId);
            var removedAsset = await db.MediaAssets.SingleAsync(x => x.Id == secondMediaAssetId);
            Assert.Null(user.ProfileImageUrl);
            Assert.Equal("INACTIVE", removedAsset.Status);
            Assert.Contains(await db.AuditLogs.ToListAsync(), audit => audit.Action == "user.profile_image_removed");
        }

        await using (var db = CreateDb(harness.ConnectionString))
        {
            var service = CreateService(db);
            var wrongTenantResult = await service.CreateAsync(
                CreateContext(fixture, [TenantAdminUserPermissions.Create]),
                CreateRequest(fixture.RoleId, wrongTenantMediaAssetId) with { Email = "wrong.media@example.com" },
                CancellationToken.None,
                "wrong-tenant-profile-media");
            var expiredResult = await service.CreateAsync(
                CreateContext(fixture, [TenantAdminUserPermissions.Create]),
                CreateRequest(fixture.RoleId, expiredMediaAssetId) with { Email = "expired.media@example.com" },
                CancellationToken.None,
                "expired-profile-media");

            Assert.True(wrongTenantResult.IsFailure);
            Assert.Equal("user.profile_media_wrong_tenant", wrongTenantResult.Error.Code);
            Assert.True(expiredResult.IsFailure);
            Assert.Equal("user.profile_media_expired", expiredResult.Error.Code);
        }
    }

    private static TenantAdminUserService CreateService(EPosDbContext db) =>
        new(
            new IdempotencyService(db, new FixedDateTimeProvider(Now)),
            new TenantAdminUserRepository(db),
            new FixedDateTimeProvider(Now),
            new ThrowingPasswordHashService(),
            new AllowingTenantResourceLimitGuard(),
            new TenantUserStaffCodeService(db),
            new FakeInvitationTokenService(),
            new Lazy<IInvitationDeliverySecretProtector>(() => new FakeDeliverySecretProtector()));

    private static TenantRequestContext CreateContext(FixtureIds fixture, IReadOnlyCollection<string> permissions) =>
        new(fixture.TenantId, fixture.ActorUserId, permissions);

    private static TenantAdminUserCreateRequest CreateRequest(Guid roleId, Guid? mediaAssetId) =>
        new(
            "Profile Media User",
            "profile.media@example.com",
            null,
            roleId,
            [],
            false,
            [],
            false,
            ProfileMediaAssetId: mediaAssetId,
            AccountStatus: TenantUserConstants.StatusInactive);

    private static TenantAdminUserUpdateRequest UpdateRequest(Guid roleId) =>
        new(
            "Profile Media User",
            "profile.media@example.com",
            null,
            roleId,
            [],
            false,
            [],
            TenantUserConstants.StatusInactive);

    private static MediaAsset CreateImageAsset(Guid id, Guid tenantId, string fileName, string publicUrl) =>
        MediaAsset.Create(
            id,
            tenantId,
            "tenant-media",
            $"tenant-users/{id:N}/{fileName}",
            publicUrl,
            fileName,
            "image/jpeg",
            ".jpg",
            1024,
            120,
            120,
            $"hash-{id:N}",
            "IMAGE",
            "TENANT_USER_PROFILE_IMAGE",
            "ACTIVE",
            null,
            Now);

    private static async Task<FixtureIds> SeedFoundationAsync(string connectionString)
    {
        await using var db = CreateDb(connectionString);
        var fixture = FixtureIds.Create();

        db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
        db.Tenants.Add(Tenant.Create(
            fixture.TenantId,
            $"PMT-{fixture.Suffix}",
            $"pmt-{fixture.Suffix}",
            "Profile Media Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));
        db.Tenants.Add(Tenant.Create(
            fixture.OtherTenantId,
            $"PMO-{fixture.Suffix}",
            $"pmo-{fixture.Suffix}",
            "Other Profile Media Tenant",
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
            "Profile Media Role",
            "Role for profile media tests.",
            true,
            true,
            null,
            Now));
        db.TenantUsers.Add(TenantUser.Create(
            fixture.ActorUserId,
            fixture.TenantId,
            $"actor-{fixture.Suffix}@example.com",
            "Actor User",
            null,
            null,
            "hash",
            "salt",
            TenantUserConstants.StatusActive,
            "admin",
            "admin",
            null,
            Now,
            staffCode: $"USR-{Now:yyyy}-99999"));

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
            var databaseName = $"tenant_user_profile_media_{Guid.NewGuid():N}";
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

    private sealed record FixtureIds(Guid TenantId, Guid OtherTenantId, Guid RoleId, Guid ActorUserId, string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new FixtureIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class ThrowingPasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => throw new InvalidOperationException("Password hashing is not used.");
        public bool VerifyPassword(string password, string passwordHash) => throw new InvalidOperationException("Password verification is not used.");
    }

    private sealed class FakeInvitationTokenService : IInvitationTokenService
    {
        public string GenerateToken() => "profile-media-token";
        public string HashToken(string rawToken) => "profile-media-token-hash";
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) => new("cipher:" + rawToken, "test");
        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }
}
