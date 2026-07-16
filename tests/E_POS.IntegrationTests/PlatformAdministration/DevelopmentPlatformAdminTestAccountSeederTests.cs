using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class DevelopmentPlatformAdminTestAccountSeederTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    private const string BillingViewerEmail = "billing.viewer.dev@local.test";
    private const string NoBillingEmail = "billing.none.dev@local.test";
    private const string BillingViewerPassword = "BillingViewer-Test-Secret-1";
    private const string NoBillingPassword = "NoBilling-Test-Secret-1";

    private static readonly Guid BillingViewerRoleId = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid NoBillingRoleId = Guid.Parse("71000000-0000-0000-0000-000000000002");
    private static readonly Guid WrongRoleId = Guid.Parse("71000000-0000-0000-0000-000000000099");

    [Fact]
    public async Task SeedAsync_CreatesBillingViewerAndNoBillingAccountsWhenAbsent()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync();

        var viewer = await FindUserAsync(dbContext, BillingViewerEmail);
        var noBilling = await FindUserAsync(dbContext, NoBillingEmail);

        Assert.NotNull(viewer);
        Assert.NotNull(noBilling);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, viewer!.Status);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, noBilling!.Status);
        Assert.False(PlatformUserProtection.IsPendingInvite(viewer));
        Assert.False(PlatformUserProtection.IsPendingInvite(noBilling));
        Assert.False(await HasSuperAdminRoleAsync(dbContext, viewer.Id));
        Assert.False(await HasSuperAdminRoleAsync(dbContext, noBilling.Id));

        var hasher = new PasswordHashService();
        Assert.True(hasher.VerifyPassword(BillingViewerPassword, viewer.PasswordHash));
        Assert.True(hasher.VerifyPassword(NoBillingPassword, noBilling.PasswordHash));
    }

    [Fact]
    public async Task SeedAsync_ReusesInvitePendingAccountsAndClearsPendingState()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);

        var pendingViewerId = Guid.NewGuid();
        var pendingNoBillingId = Guid.NewGuid();
        dbContext.PlatformUsers.AddRange(
            PlatformUser.CreatePendingInvite(pendingViewerId, BillingViewerEmail, Now),
            PlatformUser.CreatePendingInvite(pendingNoBillingId, NoBillingEmail, Now));
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync();

        var viewer = await FindUserAsync(dbContext, BillingViewerEmail);
        var noBilling = await FindUserAsync(dbContext, NoBillingEmail);

        Assert.NotNull(viewer);
        Assert.NotNull(noBilling);
        Assert.Equal(pendingViewerId, viewer!.Id);
        Assert.Equal(pendingNoBillingId, noBilling!.Id);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, viewer.Status);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, noBilling.Status);
        Assert.False(PlatformUserProtection.IsPendingInvite(viewer));
        Assert.False(PlatformUserProtection.IsPendingInvite(noBilling));
        Assert.NotEqual(PlatformUserConstants.PendingInvitePasswordHash, viewer.PasswordHash);

        var hasher = new PasswordHashService();
        Assert.True(hasher.VerifyPassword(BillingViewerPassword, viewer.PasswordHash));
        Assert.True(hasher.VerifyPassword(NoBillingPassword, noBilling.PasswordHash));

        var authService = CreateAuthService(dbContext);
        var viewerLogin = await authService.LoginAsync(
            new PlatformAdminLoginRequest(BillingViewerEmail, BillingViewerPassword),
            CancellationToken.None);
        var noBillingLogin = await authService.LoginAsync(
            new PlatformAdminLoginRequest(NoBillingEmail, NoBillingPassword),
            CancellationToken.None);

        Assert.True(viewerLogin.IsSuccess);
        Assert.True(noBillingLogin.IsSuccess);
    }

    [Fact]
    public async Task SeedAsync_AssignsExpectedRolesAndPermissions()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync();

        var viewer = await FindUserAsync(dbContext, BillingViewerEmail);
        var noBilling = await FindUserAsync(dbContext, NoBillingEmail);
        Assert.NotNull(viewer);
        Assert.NotNull(noBilling);

        var repository = new PlatformUserRepository(dbContext);
        var viewerRoles = await repository.GetUserActiveRoleCodesAsync(viewer!.Id, CancellationToken.None);
        var noBillingRoles = await repository.GetUserActiveRoleCodesAsync(noBilling!.Id, CancellationToken.None);

        Assert.Equal([PlatformRoleCodes.BillingViewerDev], viewerRoles);
        Assert.Equal([PlatformRoleCodes.PlatformOpsNoBillingDev], noBillingRoles);

        var authRepository = new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext));
        var viewerPermissions = await authRepository.GetActivePermissionCodesAsync(viewer.Id, CancellationToken.None);
        var noBillingPermissions = await authRepository.GetActivePermissionCodesAsync(noBilling.Id, CancellationToken.None);

        Assert.Contains(PlatformPermissionCodes.BillingView, viewerPermissions);
        Assert.Contains(PlatformPermissionCodes.DashboardView, viewerPermissions);
        Assert.DoesNotContain(PlatformPermissionCodes.BillingManage, viewerPermissions);

        Assert.Contains(PlatformPermissionCodes.DashboardView, noBillingPermissions);
        Assert.Contains(PlatformPermissionCodes.TenantsView, noBillingPermissions);
        Assert.DoesNotContain(PlatformPermissionCodes.BillingView, noBillingPermissions);
        Assert.DoesNotContain(PlatformPermissionCodes.BillingManage, noBillingPermissions);
    }

    [Fact]
    public async Task SeedAsync_ReconcilesIncorrectRoleAssignmentWithoutDuplicates()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);

        var userId = Guid.NewGuid();
        var user = PlatformUser.Create(
            userId,
            BillingViewerEmail,
            new PasswordHashService().HashPassword("old-password"),
            PlatformAuthConstants.ActiveStatus,
            Now);
        dbContext.PlatformUsers.Add(user);
        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.NewGuid(),
            userId,
            WrongRoleId,
            "Incorrect test role.",
            Now));
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var activeAssignments = await dbContext.PlatformUserRoles
            .Where(assignment => assignment.PlatformUserId == userId && assignment.RevokedAt == null)
            .ToListAsync();

        Assert.Single(activeAssignments);
        Assert.Equal(BillingViewerRoleId, activeAssignments[0].PlatformRoleId);
        Assert.Equal(1, await dbContext.PlatformUsers.CountAsync(item => item.NormalizedEmail == PlatformUser.NormalizeEmail(BillingViewerEmail)));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotentAcrossRepeatedRuns()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);
        var seeder = CreateSeeder(dbContext);

        await seeder.SeedAsync();
        var firstViewer = await FindUserAsync(dbContext, BillingViewerEmail);
        var firstNoBilling = await FindUserAsync(dbContext, NoBillingEmail);
        Assert.NotNull(firstViewer);
        Assert.NotNull(firstNoBilling);

        await seeder.SeedAsync();

        Assert.Equal(2, await dbContext.PlatformUsers.CountAsync(user =>
            user.NormalizedEmail == PlatformUser.NormalizeEmail(BillingViewerEmail) ||
            user.NormalizedEmail == PlatformUser.NormalizeEmail(NoBillingEmail)));

        var secondViewer = await FindUserAsync(dbContext, BillingViewerEmail);
        var secondNoBilling = await FindUserAsync(dbContext, NoBillingEmail);
        Assert.Equal(firstViewer!.Id, secondViewer!.Id);
        Assert.Equal(firstNoBilling!.Id, secondNoBilling!.Id);

        var activeViewerRoles = await dbContext.PlatformUserRoles.CountAsync(item =>
            item.PlatformUserId == firstViewer.Id && item.RevokedAt == null);
        var activeNoBillingRoles = await dbContext.PlatformUserRoles.CountAsync(item =>
            item.PlatformUserId == firstNoBilling.Id && item.RevokedAt == null);

        Assert.Equal(1, activeViewerRoles);
        Assert.Equal(1, activeNoBillingRoles);
    }

    [Fact]
    public async Task SeedAsync_DoesNotModifyExistingSuperAdministrator()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);

        var superAdmin = await dbContext.PlatformUsers.SingleAsync(user =>
            user.Id == PlatformAdminSeedConstants.DevelopmentPlatformUserId);
        var originalHash = superAdmin.PasswordHash;
        var originalStatus = superAdmin.Status;
        var originalRoleIds = await dbContext.PlatformUserRoles
            .Where(item => item.PlatformUserId == superAdmin.Id && item.RevokedAt == null)
            .Select(item => item.PlatformRoleId)
            .OrderBy(id => id)
            .ToListAsync();
        var originalPermissionCount = (await new PlatformAuthRepository(
                dbContext,
                new PlatformPermissionRepository(dbContext))
            .GetActivePermissionCodesAsync(superAdmin.Id, CancellationToken.None)).Count;

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await dbContext.Entry(superAdmin).ReloadAsync();
        Assert.Equal(originalHash, superAdmin.PasswordHash);
        Assert.Equal(originalStatus, superAdmin.Status);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, superAdmin.Status);

        var roleCodes = await new PlatformUserRepository(dbContext)
            .GetUserActiveRoleCodesAsync(superAdmin.Id, CancellationToken.None);
        Assert.Equal([PlatformRoleCodes.SuperAdministrator], roleCodes);

        var currentRoleIds = await dbContext.PlatformUserRoles
            .Where(item => item.PlatformUserId == superAdmin.Id && item.RevokedAt == null)
            .Select(item => item.PlatformRoleId)
            .OrderBy(id => id)
            .ToListAsync();
        Assert.Equal(originalRoleIds, currentRoleIds);

        var currentPermissionCount = (await new PlatformAuthRepository(
                dbContext,
                new PlatformPermissionRepository(dbContext))
            .GetActivePermissionCodesAsync(superAdmin.Id, CancellationToken.None)).Count;
        Assert.Equal(originalPermissionCount, currentPermissionCount);
    }

    [Fact]
    public async Task SeedAsync_MissingConfiguration_DoesNotThrowAndLeavesUsersUnchanged()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);
        var beforeCount = await dbContext.PlatformUsers.CountAsync();

        var seeder = CreateSeeder(
            dbContext,
            new DevelopmentPlatformAdminSeedOptions());

        var exception = await Record.ExceptionAsync(() => seeder.SeedAsync());

        Assert.Null(exception);
        Assert.Equal(beforeCount, await dbContext.PlatformUsers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_MissingRequiredRole_SkipsProfileWithoutFailing()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminOnlyAsync(dbContext);

        var seeder = CreateSeeder(dbContext);
        var exception = await Record.ExceptionAsync(() => seeder.SeedAsync());

        Assert.Null(exception);
        Assert.Null(await FindUserAsync(dbContext, BillingViewerEmail));
        Assert.Null(await FindUserAsync(dbContext, NoBillingEmail));
    }

    [Fact]
    public async Task Login_AfterSeed_ReturnsExpectedBillingPermissions()
    {
        await using var dbContext = CreateDbContext();
        await SeedFoundationAsync(dbContext);
        await CreateSeeder(dbContext).SeedAsync();

        var authService = CreateAuthService(dbContext);

        var viewerLogin = await authService.LoginAsync(
            new PlatformAdminLoginRequest(BillingViewerEmail, BillingViewerPassword),
            CancellationToken.None);
        var noBillingLogin = await authService.LoginAsync(
            new PlatformAdminLoginRequest(NoBillingEmail, NoBillingPassword),
            CancellationToken.None);
        var superAdminLogin = await authService.LoginAsync(
            new PlatformAdminLoginRequest(
                DevelopmentPlatformAdminSeedData.Email,
                DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(viewerLogin.IsSuccess);
        Assert.Contains(PlatformPermissionCodes.BillingView, viewerLogin.Value!.Permissions);
        Assert.DoesNotContain(PlatformPermissionCodes.BillingManage, viewerLogin.Value.Permissions);

        Assert.True(noBillingLogin.IsSuccess);
        Assert.DoesNotContain(PlatformPermissionCodes.BillingView, noBillingLogin.Value!.Permissions);
        Assert.DoesNotContain(PlatformPermissionCodes.BillingManage, noBillingLogin.Value.Permissions);

        Assert.True(superAdminLogin.IsSuccess);
        Assert.Contains(PlatformPermissionCodes.BillingView, superAdminLogin.Value!.Permissions);
        Assert.Contains(PlatformPermissionCodes.BillingManage, superAdminLogin.Value.Permissions);
    }

    private static DevelopmentPlatformAdminTestAccountSeeder CreateSeeder(
        EPosDbContext dbContext,
        DevelopmentPlatformAdminSeedOptions? options = null)
    {
        options ??= new DevelopmentPlatformAdminSeedOptions
        {
            BillingViewer = new DevelopmentPlatformAdminAccountOptions
            {
                Email = BillingViewerEmail,
                Password = BillingViewerPassword,
                DisplayName = DevelopmentPlatformAdminSeedOptions.DefaultBillingViewerDisplayName
            },
            NoBilling = new DevelopmentPlatformAdminAccountOptions
            {
                Email = NoBillingEmail,
                Password = NoBillingPassword,
                DisplayName = DevelopmentPlatformAdminSeedOptions.DefaultNoBillingDisplayName
            }
        };

        return new DevelopmentPlatformAdminTestAccountSeeder(
            Options.Create(options),
            new PlatformUserRepository(dbContext),
            new PasswordHashService(),
            new FixedDateTimeProvider(),
            NullLogger<DevelopmentPlatformAdminTestAccountSeeder>.Instance);
    }

    private static PlatformAuthService CreateAuthService(EPosDbContext dbContext)
    {
        return new PlatformAuthService(
            new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)),
            new PlatformAuthRequestValidator(),
            new PasswordHashService(),
            new JwtTokenFactory(new FixedDateTimeProvider()),
            new RefreshTokenGenerator(new FixedDateTimeProvider()),
            new TokenHashService(),
            new FixedDateTimeProvider(),
            JwtSettings);
    }

    private static async Task SeedFoundationAsync(EPosDbContext dbContext)
    {
        await SeedSuperAdminOnlyAsync(dbContext);
        await SeedDevRolesAsync(dbContext);
    }

    private static async Task SeedSuperAdminOnlyAsync(EPosDbContext dbContext)
    {
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            PlatformAdminSeedConstants.DevelopmentPlatformUserId,
            DevelopmentPlatformAdminSeedData.Email,
            DevelopmentPlatformAdminSeedData.PasswordHash,
            PlatformAuthConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);
    }

    private static async Task SeedDevRolesAsync(EPosDbContext dbContext)
    {
        dbContext.PlatformRoles.AddRange(
            PlatformRole.Create(
                BillingViewerRoleId,
                PlatformRoleCodes.BillingViewerDev,
                "Billing Viewer Dev",
                "Development billing viewer role.",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformRole.Create(
                NoBillingRoleId,
                PlatformRoleCodes.PlatformOpsNoBillingDev,
                "Platform Ops No Billing Dev",
                "Development ops role without billing.",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformRole.Create(
                WrongRoleId,
                "wrong_role_dev",
                "Wrong Role Dev",
                "Incorrect role used for reconciliation tests.",
                PlatformAuthConstants.ActiveStatus,
                Now));
        await dbContext.SaveChangesAsync();

        await GrantRolePermissionsAsync(
            dbContext,
            BillingViewerRoleId,
            [
                PlatformPermissionCodes.BillingView,
                PlatformPermissionCodes.DashboardView
            ]);

        await GrantRolePermissionsAsync(
            dbContext,
            NoBillingRoleId,
            [
                PlatformPermissionCodes.DashboardView,
                PlatformPermissionCodes.TenantsView
            ]);
    }

    private static async Task GrantRolePermissionsAsync(
        EPosDbContext dbContext,
        Guid roleId,
        IReadOnlyList<string> permissionCodes)
    {
        var permissions = await dbContext.PlatformPermissions
            .Where(permission => permissionCodes.Contains(permission.PermissionCode))
            .ToListAsync();

        foreach (var permission in permissions)
        {
            dbContext.PlatformRolePermissions.Add(PlatformRolePermission.Create(
                Guid.NewGuid(),
                roleId,
                permission.Id,
                "Development test role permission.",
                Now));
        }

        await dbContext.SaveChangesAsync();
    }

    private static Task<PlatformUser?> FindUserAsync(EPosDbContext dbContext, string email)
    {
        var normalized = PlatformUser.NormalizeEmail(email);
        return dbContext.PlatformUsers.FirstOrDefaultAsync(user => user.NormalizedEmail == normalized);
    }

    private static Task<bool> HasSuperAdminRoleAsync(EPosDbContext dbContext, Guid userId)
    {
        return new PlatformUserRepository(dbContext)
            .UserHasActiveRoleCodeAsync(userId, PlatformRoleCodes.SuperAdministrator, CancellationToken.None);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
