using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class DevelopmentPlatformAdminTestAccountSeederUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SeedAsync_MissingPassword_SkipsProfileWithoutThrowing()
    {
        var repository = new TrackingUserRepository();
        var logger = new CapturingLogger();
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                BillingViewer = new DevelopmentPlatformAdminAccountOptions
                {
                    Email = "billing.viewer.dev@local.test"
                }
            },
            repository,
            logger);

        await seeder.SeedAsync();

        Assert.False(repository.AddCalled);
        Assert.False(repository.UpdateCalled);
        Assert.Contains(logger.Messages, message =>
            message.Contains("BillingViewer", StringComparison.Ordinal) &&
            message.Contains("skipped", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeedAsync_MissingEmail_SkipsProfileWithoutThrowing()
    {
        var repository = new TrackingUserRepository();
        var logger = new CapturingLogger();
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                NoBilling = new DevelopmentPlatformAdminAccountOptions
                {
                    Password = "NoBillingSecret-1"
                }
            },
            repository,
            logger);

        await seeder.SeedAsync();

        Assert.False(repository.AddCalled);
        Assert.Contains(logger.Messages, message =>
            message.Contains("NoBilling", StringComparison.Ordinal) &&
            message.Contains("skipped", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeedAsync_DoesNotLogPasswordOrPasswordHash()
    {
        var roleId = Guid.NewGuid();
        var repository = new TrackingUserRepository
        {
            ResolvedRoles =
            [
                new ResolvedPlatformRole(roleId, PlatformRoleCodes.BillingViewerDev, "Billing Viewer Dev")
            ]
        };
        var logger = new CapturingLogger();
        const string password = "ViewerSecret-DoNotLog";
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                BillingViewer = new DevelopmentPlatformAdminAccountOptions
                {
                    Email = "billing.viewer.dev@local.test",
                    Password = password
                }
            },
            repository,
            logger);

        await seeder.SeedAsync();

        Assert.True(repository.AddCalled);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(password, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message =>
            message.Contains("PBKDF2-SHA256", StringComparison.Ordinal) ||
            message.Contains(PlatformUserConstants.PendingInvitePasswordHash, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SeedAsync_MissingRequiredRole_SkipsProfileWithoutMutating()
    {
        var repository = new TrackingUserRepository { ResolvedRoles = [] };
        var logger = new CapturingLogger();
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                BillingViewer = new DevelopmentPlatformAdminAccountOptions
                {
                    Email = "billing.viewer.dev@local.test",
                    Password = "ViewerSecret-1"
                }
            },
            repository,
            logger);

        await seeder.SeedAsync();

        Assert.False(repository.AddCalled);
        Assert.False(repository.UpdateCalled);
        Assert.Contains(logger.Messages, message =>
            message.Contains("BillingViewer", StringComparison.Ordinal) &&
            message.Contains(PlatformRoleCodes.BillingViewerDev, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SeedAsync_CreatesLoginCapableAccountWhenAbsent()
    {
        var roleId = Guid.NewGuid();
        var repository = new TrackingUserRepository
        {
            ResolvedRoles =
            [
                new ResolvedPlatformRole(roleId, PlatformRoleCodes.BillingViewerDev, "Billing Viewer Dev")
            ]
        };
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                BillingViewer = new DevelopmentPlatformAdminAccountOptions
                {
                    Email = "billing.viewer.dev@local.test",
                    Password = "ViewerSecret-1",
                    DisplayName = "Billing Viewer Development"
                }
            },
            repository,
            new CapturingLogger());

        await seeder.SeedAsync();

        Assert.True(repository.AddCalled);
        Assert.True(repository.UsersByNormalizedEmail.TryGetValue(
            PlatformUser.NormalizeEmail("billing.viewer.dev@local.test"),
            out var created));
        Assert.NotNull(created);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, created!.Status);
        Assert.False(PlatformUserProtection.IsPendingInvite(created));
        Assert.Equal("Billing Viewer Development", created.DisplayName);
        Assert.True(new PasswordHashService().VerifyPassword("ViewerSecret-1", created.PasswordHash));
        Assert.Equal([roleId], repository.LastAddedRoleIds);
    }

    [Fact]
    public async Task SeedAsync_ReusesInvitePendingAccountPreservingId()
    {
        var roleId = Guid.NewGuid();
        var pendingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var pending = PlatformUser.CreatePendingInvite(
            pendingId,
            "billing.none.dev@local.test",
            Now);
        var repository = new TrackingUserRepository
        {
            UsersByNormalizedEmail =
            {
                [PlatformUser.NormalizeEmail("billing.none.dev@local.test")] = pending
            },
            ResolvedRoles =
            [
                new ResolvedPlatformRole(roleId, PlatformRoleCodes.PlatformOpsNoBillingDev, "No Billing Dev")
            ]
        };
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                NoBilling = new DevelopmentPlatformAdminAccountOptions
                {
                    Email = "billing.none.dev@local.test",
                    Password = "NoBillingSecret-1"
                }
            },
            repository,
            new CapturingLogger());

        await seeder.SeedAsync();

        Assert.False(repository.AddCalled);
        Assert.True(repository.UpdateCalled);
        Assert.True(repository.ReplaceRolesCalled);
        Assert.Equal(pendingId, pending.Id);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, pending.Status);
        Assert.False(PlatformUserProtection.IsPendingInvite(pending));
        Assert.Equal([roleId], repository.LastReplacedRoleIds);
        Assert.True(new PasswordHashService().VerifyPassword("NoBillingSecret-1", pending.PasswordHash));
    }

    [Fact]
    public async Task SeedAsync_ProtectedSuperAdminEmail_DoesNotMutateAccount()
    {
        var superAdmin = PlatformUser.Create(
            PlatformAdminSeedConstants.DevelopmentPlatformUserId,
            DevelopmentPlatformAdminSeedData.Email,
            DevelopmentPlatformAdminSeedData.PasswordHash,
            PlatformAuthConstants.ActiveStatus,
            Now);
        var originalHash = superAdmin.PasswordHash;

        var repository = new TrackingUserRepository
        {
            UsersByNormalizedEmail =
            {
                [PlatformAdminSeedConstants.DevelopmentPlatformUserEmail] = superAdmin
            },
            ResolvedRoles =
            [
                new ResolvedPlatformRole(Guid.NewGuid(), PlatformRoleCodes.BillingViewerDev, "Billing Viewer Dev")
            ]
        };
        var logger = new CapturingLogger();
        var seeder = CreateSeeder(
            new DevelopmentPlatformAdminSeedOptions
            {
                BillingViewer = new DevelopmentPlatformAdminAccountOptions
                {
                    Email = DevelopmentPlatformAdminSeedData.Email,
                    Password = "ShouldNeverApply"
                }
            },
            repository,
            logger);

        await seeder.SeedAsync();

        Assert.False(repository.AddCalled);
        Assert.False(repository.UpdateCalled);
        Assert.False(repository.ReplaceRolesCalled);
        Assert.Equal(originalHash, superAdmin.PasswordHash);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, superAdmin.Status);
        Assert.Contains(logger.Messages, message =>
            message.Contains("protected", StringComparison.OrdinalIgnoreCase));
    }

    private static DevelopmentPlatformAdminTestAccountSeeder CreateSeeder(
        DevelopmentPlatformAdminSeedOptions options,
        TrackingUserRepository repository,
        CapturingLogger logger)
    {
        return new DevelopmentPlatformAdminTestAccountSeeder(
            Options.Create(options),
            repository,
            new PasswordHashService(),
            new FixedDateTimeProvider(),
            logger);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TrackingUserRepository : IPlatformUserRepository
    {
        public Dictionary<string, PlatformUser> UsersByNormalizedEmail { get; } = new(StringComparer.Ordinal);
        public IReadOnlyList<ResolvedPlatformRole> ResolvedRoles { get; init; } = [];
        public bool AddCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool ReplaceRolesCalled { get; private set; }
        public IReadOnlyList<Guid> LastAddedRoleIds { get; private set; } = [];
        public IReadOnlyList<Guid> LastReplacedRoleIds { get; private set; } = [];

        public Task<PlatformUserListResponse> GetUsersAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformUserListResponse([]));

        public Task<PlatformUserDetailResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<PlatformUserDetailResponse?>(null);

        public Task<PlatformUser?> GetUserEntityByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(UsersByNormalizedEmail.Values.FirstOrDefault(user => user.Id == userId));

        public Task<PlatformUser?> GetUserEntityByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            UsersByNormalizedEmail.TryGetValue(normalizedEmail, out var user);
            return Task.FromResult(user);
        }

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            Guid? excludingUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(UsersByNormalizedEmail.ContainsKey(normalizedEmail));

        public Task AddUserWithRolesAsync(
            PlatformUser user,
            IReadOnlyList<Guid> roleIds,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            AddCalled = true;
            LastAddedRoleIds = roleIds.ToList();
            UsersByNormalizedEmail[user.NormalizedEmail] = user;
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(PlatformUser user, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            UsersByNormalizedEmail[user.NormalizedEmail] = user;
            return Task.CompletedTask;
        }

        public Task ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyList<Guid> roleIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            CancellationToken cancellationToken)
        {
            ReplaceRolesCalled = true;
            LastReplacedRoleIds = roleIds.ToList();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ResolvedPlatformRole>> ResolveActiveRolesAsync(
            IReadOnlyList<Guid>? roleIds,
            IReadOnlyList<string>? roleCodes,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResolvedRoles);

        public Task<bool> UserHasActiveRoleCodeAsync(
            Guid userId,
            string roleCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<int> CountActiveSuperAdministratorsAsync(
            Guid? excludingUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<IReadOnlyList<string>> GetUserActiveRoleCodesAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class CapturingLogger : ILogger<DevelopmentPlatformAdminTestAccountSeeder>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
