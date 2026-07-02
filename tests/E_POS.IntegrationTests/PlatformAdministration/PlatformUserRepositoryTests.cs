using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;
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
        Assert.Equal(31, superAdmin.PermissionCount);
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
        await repository.ReplaceUserRolesAsync(userId, [replacementRoleId], Now, CancellationToken.None);

        var roleCodes = await repository.GetUserActiveRoleCodesAsync(userId, CancellationToken.None);

        Assert.Equal(["replacement_role"], roleCodes);
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
