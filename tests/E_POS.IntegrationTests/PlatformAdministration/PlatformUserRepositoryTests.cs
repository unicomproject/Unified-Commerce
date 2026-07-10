using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformUserRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetUsersAsync_ReadsSeededDevelopmentSuperAdministrator()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);

        var users = await repository.GetUsersAsync(CancellationToken.None);

        Assert.NotEmpty(users.Users);
        var superAdmin = users.Users.Single(user =>
            user.Id == PlatformAdminSeedConstants.DevelopmentPlatformUserId);

        Assert.Equal(DevelopmentPlatformAdminSeedData.Email, superAdmin.Email);
        Assert.Contains(PlatformRoleCodes.SuperAdministrator, superAdmin.RoleCodes);
        Assert.Equal(36, superAdmin.PermissionCount);
    }

    [Fact]
    public async Task GetUsersAsync_UsesLoginStatusAndAttemptedAtForLastLogin()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        var userId = PlatformAdminSeedConstants.DevelopmentPlatformUserId;
        var successfulAudit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.SuccessLoginResult,
            Now,
            attemptedAt: Now.AddMinutes(5));
        var failedAudit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.FailedLoginResult,
            Now,
            attemptedAt: Now.AddMinutes(30));

        dbContext.PlatformLoginAudits.AddRange(successfulAudit, failedAudit);
        await dbContext.SaveChangesAsync();

        dbContext.Entry(successfulAudit).Property(nameof(PlatformLoginAudit.LoginResult)).CurrentValue = PlatformAuthConstants.FailedLoginResult;
        dbContext.Entry(successfulAudit).Property(nameof(PlatformLoginAudit.CreatedAt)).CurrentValue = Now.AddHours(3);
        await dbContext.SaveChangesAsync();

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);
        var users = await repository.GetUsersAsync(CancellationToken.None);
        var superAdmin = users.Users.Single(user => user.Id == userId);

        Assert.Equal(Now.AddMinutes(5), superAdmin.LastLoginAt);
    }

    [Fact]
    public async Task AddUserWithRolesAsync_PersistsPendingInviteUserAndRoles()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var roleId = Guid.NewGuid();
        dbContext.PlatformRoles.Add(PlatformRole.Create(
            roleId,
            "integration_support_role",
            "Integration Support Role",
            "Integration test role.",
            PlatformAuthConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var user = PlatformUser.CreatePendingInvite(userId, "integration.user@example.com", Now);

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);
        await repository.AddUserWithRolesAsync(user, [roleId], Now, CancellationToken.None);

        var detail = await repository.GetUserByIdAsync(userId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(PlatformAuthConstants.InactiveStatus, detail!.Status);
        Assert.True(detail.InvitePending);
        Assert.Equal(["integration_support_role"], detail.RoleCodes);
    }

    [Fact]
    public async Task ReplaceUserRolesAsync_UpdatesAssignedRoles()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var initialRoleId = Guid.NewGuid();
        var replacementRoleId = Guid.NewGuid();
        dbContext.PlatformRoles.AddRange(
            PlatformRole.Create(
                initialRoleId,
                "initial_role",
                "Initial Role",
                "Initial role.",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformRole.Create(
                replacementRoleId,
                "replacement_role",
                "Replacement Role",
                "Replacement role.",
                PlatformAuthConstants.ActiveStatus,
                Now));
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var user = PlatformUser.CreatePendingInvite(userId, "role.change@example.com", Now);
        dbContext.PlatformUsers.Add(user);
        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.NewGuid(),
            userId,
            initialRoleId,
            "Initial assignment.",
            Now));
        await dbContext.SaveChangesAsync();

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);
        await repository.ReplaceUserRolesAsync(userId, [replacementRoleId], Now, null, CancellationToken.None);

        var roleCodes = await repository.GetUserActiveRoleCodesAsync(userId, CancellationToken.None);

        Assert.Equal(["replacement_role"], roleCodes);
    }

    [Fact]
    public async Task ReplaceUserRolesAsync_RevokesRemovedRolesAndPreservesHistory()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var initialRoleId = Guid.NewGuid();
        var replacementRoleId = Guid.NewGuid();
        dbContext.PlatformRoles.AddRange(
            PlatformRole.Create(
                initialRoleId,
                "initial_role",
                "Initial Role",
                "Initial role.",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformRole.Create(
                replacementRoleId,
                "replacement_role",
                "Replacement Role",
                "Replacement role.",
                PlatformAuthConstants.ActiveStatus,
                Now));
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        dbContext.PlatformUsers.Add(PlatformUser.CreatePendingInvite(userId, "soft.revoke@example.com", Now));
        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.NewGuid(),
            userId,
            initialRoleId,
            "Initial assignment.",
            Now));
        await dbContext.SaveChangesAsync();

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);
        await repository.ReplaceUserRolesAsync(userId, [replacementRoleId], Now, actorId, CancellationToken.None);

        var assignments = await dbContext.PlatformUserRoles
            .Where(item => item.PlatformUserId == userId)
            .ToListAsync();

        Assert.Equal(2, assignments.Count);
        Assert.Single(assignments, item => item.PlatformRoleId == initialRoleId && item.RevokedAt != null);
        Assert.Single(assignments, item => item.PlatformRoleId == replacementRoleId && item.RevokedAt == null);
    }

    [Fact]
    public async Task ReplaceUserRolesAsync_ReassignAfterRevokeCreatesNewActiveRow()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var roleId = Guid.NewGuid();
        dbContext.PlatformRoles.Add(PlatformRole.Create(
            roleId,
            "reassign_role",
            "Reassign Role",
            "Reassign role.",
            PlatformAuthConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var revokedAssignmentId = Guid.NewGuid();
        dbContext.PlatformUsers.Add(PlatformUser.CreatePendingInvite(userId, "reassign@example.com", Now));

        var revokedAssignment = PlatformUserRole.Create(
            revokedAssignmentId,
            userId,
            roleId,
            "Initial assignment.",
            Now);
        revokedAssignment.Revoke(Guid.NewGuid(), "Previous revocation.", Now);
        dbContext.PlatformUserRoles.Add(revokedAssignment);
        await dbContext.SaveChangesAsync();

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);
        await repository.ReplaceUserRolesAsync(userId, [roleId], Now, null, CancellationToken.None);

        var assignments = await dbContext.PlatformUserRoles
            .Where(item => item.PlatformUserId == userId)
            .ToListAsync();

        Assert.Equal(2, assignments.Count);
        Assert.Single(assignments, item => item.Id == revokedAssignmentId && item.RevokedAt != null);
        Assert.Single(assignments, item => item.Id != revokedAssignmentId && item.RevokedAt == null);
    }

    [Fact]
    public async Task GetUserActiveRoleCodesAsync_IgnoresRevokedAssignments()
    {
        await using var dbContext = CreateDbContext();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        var activeRoleId = Guid.NewGuid();
        var revokedRoleId = Guid.NewGuid();
        dbContext.PlatformRoles.AddRange(
            PlatformRole.Create(
                activeRoleId,
                "active_role",
                "Active Role",
                "Active role.",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformRole.Create(
                revokedRoleId,
                "revoked_role",
                "Revoked Role",
                "Revoked role.",
                PlatformAuthConstants.ActiveStatus,
                Now));
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();
        dbContext.PlatformUsers.Add(PlatformUser.CreatePendingInvite(userId, "active.only@example.com", Now));
        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.NewGuid(),
            userId,
            activeRoleId,
            "Active assignment.",
            Now));

        var revokedAssignment = PlatformUserRole.Create(
            Guid.NewGuid(),
            userId,
            revokedRoleId,
            "Revoked assignment.",
            Now);
        revokedAssignment.Revoke(null, "Revoked for test.", Now);
        dbContext.PlatformUserRoles.Add(revokedAssignment);
        await dbContext.SaveChangesAsync();

        IPlatformUserRepository repository = new PlatformUserRepository(dbContext);

        var roleCodes = await repository.GetUserActiveRoleCodesAsync(userId, CancellationToken.None);

        Assert.Equal(["active_role"], roleCodes);
    }

    private static async Task SeedSuperAdminAsync(EPosDbContext dbContext)
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

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}



