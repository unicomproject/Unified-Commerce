using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformAuthRepositoryTests
{
    private const string PostgreSqlConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveFailedCredentialAttemptAsync_UsesLoginStatusEvenWhenLoginResultIsTampered()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            userId,
            $"lockout-{userId:N}@example.test",
            "HASHED:test-password",
            PlatformAuthConstants.ActiveStatus,
            Now));

        var previousFailedAudit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.FailedLoginResult,
            Now.AddMinutes(-5),
            attemptedAt: Now.AddMinutes(-5));

        dbContext.PlatformLoginAudits.Add(previousFailedAudit);
        await dbContext.SaveChangesAsync();

        dbContext.Entry(previousFailedAudit).Property(nameof(PlatformLoginAudit.LoginResult)).CurrentValue = PlatformAuthConstants.SuccessLoginResult;
        await dbContext.SaveChangesAsync();

        var repository = new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext));
        var currentFailedAudit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.FailedLoginResult,
            Now,
            attemptedAt: Now);

        await repository.SaveFailedCredentialAttemptAsync(
            currentFailedAudit,
            Now.AddMinutes(-15),
            maxFailedAttempts: 2,
            CancellationToken.None);

        var user = await dbContext.PlatformUsers
            .AsNoTracking()
            .SingleAsync(x => x.Id == userId);
        Assert.Equal(PlatformAuthConstants.LockedStatus, user.Status);
        Assert.Equal(Now, user.UpdatedAt);

        await transaction.RollbackAsync();
    }

    private static async Task<bool> CanConnectToPostgreSqlAsync()
    {
        await using var dbContext = CreatePostgreSqlDbContext();
        try
        {
            return await dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private static EPosDbContext CreatePostgreSqlDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(PostgreSqlConnectionString)
            .Options;

        return new EPosDbContext(options);
    }
}
