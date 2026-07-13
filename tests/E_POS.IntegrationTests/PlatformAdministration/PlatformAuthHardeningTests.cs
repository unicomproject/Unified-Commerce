using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformAuthHardeningTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 10, 56, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedAt = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HardeningBackfill_LegacyPasswordReset_SetsRequestedAtAndExpiresAt()
    {
        var userId = Guid.NewGuid();
        var legacyToken = PlatformPasswordResetToken.CreateLegacy(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("reset"),
            PlatformAuthConstants.PendingTokenStatus,
            CreatedAt);

        legacyToken.ApplyAlignmentBackfill();
        legacyToken.ApplyHardeningBackfill();

        Assert.Equal(CreatedAt, legacyToken.RequestedAt);
        Assert.Equal(CreatedAt.AddHours(PlatformPasswordResetConstants.DefaultLifetimeHours), legacyToken.ExpiresAt);
        Assert.True(legacyToken.ExpiresAt > legacyToken.RequestedAt);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HardeningBackfill_LegacyLoginAudit_FillsRequiredAuditColumns()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        var legacyAudit = PlatformLoginAudit.CreateLegacy(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.FailedLoginResult,
            CreatedAt);
        legacyAudit.ApplyAlignmentBackfill();
        dbContext.PlatformLoginAudits.Add(legacyAudit);
        await dbContext.SaveChangesAsync();

        await PlatformAuthHardeningBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformLoginAudits.SingleAsync();
        Assert.Equal(CreatedAt, saved.AttemptedAt);
        Assert.Equal(PlatformAuthConstants.FailedLoginResult, saved.LoginStatus);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, saved.AuthenticationMethod);
    }

    [Fact]
    public async Task HardeningBackfill_LegacyRefreshToken_FillsPlatformUserIdAndFamilyId()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(
            sessionId,
            userId,
            CreateTokenHash("session"),
            CreatedAt));

        var legacyRefresh = PlatformRefreshToken.CreateLegacy(
            Guid.NewGuid(),
            sessionId,
            CreateTokenHash("refresh"),
            PlatformAuthConstants.ActiveTokenStatus,
            CreatedAt.AddDays(7),
            CreatedAt,
            UpdatedAt);
        legacyRefresh.ApplyAlignmentBackfill(userId);
        dbContext.PlatformRefreshTokens.Add(legacyRefresh);
        await dbContext.SaveChangesAsync();

        await PlatformAuthHardeningBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformRefreshTokens.SingleAsync();
        Assert.Equal(userId, saved.PlatformUserId);
        Assert.Equal(legacyRefresh.Id, saved.TokenFamilyId);
    }

    [Fact]
    public async Task HardeningBackfill_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        var legacyToken = PlatformPasswordResetToken.CreateLegacy(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("idempotent-reset"),
            PlatformAuthConstants.PendingTokenStatus,
            CreatedAt);
        legacyToken.ApplyAlignmentBackfill();
        legacyToken.ApplyHardeningBackfill();
        dbContext.PlatformPasswordResetTokens.Add(legacyToken);
        await dbContext.SaveChangesAsync();

        await PlatformAuthHardeningBackfillApplicator.ApplyAsync(dbContext);
        await PlatformAuthHardeningBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformPasswordResetTokens.SingleAsync();
        Assert.Equal(CreatedAt.AddHours(PlatformPasswordResetConstants.DefaultLifetimeHours), saved.ExpiresAt);
    }

    [Fact]
    public async Task CreatePendingResetToken_WritesNonNullRequestedAtAndExpiresAt()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        var token = PlatformPasswordResetToken.CreatePending(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("pending"),
            Now.AddHours(PlatformPasswordResetConstants.DefaultLifetimeHours),
            Now);
        dbContext.PlatformPasswordResetTokens.Add(token);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.PlatformPasswordResetTokens.SingleAsync();
        Assert.NotNull(saved.RequestedAt);
        Assert.NotNull(saved.ExpiresAt);
        Assert.True(saved.ExpiresAt > saved.RequestedAt);
    }

    [Fact]
    public async Task LoginAuditCreate_WritesNonNullLoginStatusAttemptedAtAndAuthenticationMethod()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformLoginAudits.Add(PlatformLoginAudit.Create(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.SuccessLoginResult,
            Now,
            PlatformAuthAlignmentConstants.AuthenticationMethod.Password,
            attemptedAt: Now,
            platformAuthSessionId: sessionId,
            ipAddress: "127.0.0.1",
            userAgent: "test-agent"));
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.PlatformLoginAudits.SingleAsync();
        Assert.Equal(Now, saved.AttemptedAt);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, saved.LoginStatus);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, saved.AuthenticationMethod);
    }

    [Fact]
    public async Task RefreshTokenCreate_WritesNonNullPlatformUserIdAndTokenFamilyId()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(
            sessionId,
            userId,
            CreateTokenHash("session"),
            CreatedAt));

        var refreshToken = PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionId,
            CreateTokenHash("refresh"),
            CreatedAt.AddDays(7),
            CreatedAt,
            platformUserId: userId);
        dbContext.PlatformRefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.PlatformRefreshTokens.SingleAsync();
        Assert.Equal(userId, saved.PlatformUserId);
        Assert.Equal(refreshToken.Id, saved.TokenFamilyId);
    }

    private static void SeedUser(EPosDbContext dbContext, Guid userId)
    {
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            userId,
            $"user-{userId:N}@example.test",
            "HASHED:test-password",
            PlatformAuthConstants.ActiveStatus,
            Now));
    }

    private static string CreateTokenHash(string label) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{label}:{Guid.NewGuid():N}"));

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
