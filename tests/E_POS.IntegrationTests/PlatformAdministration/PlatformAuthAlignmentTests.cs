using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformAuthAlignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 6, 20, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedAt = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Backfill_SetsRevokedAt_ForRevokedSessions()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        var session = PlatformAuthSession.CreateLegacy(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("session"),
            PlatformAuthConstants.RevokedTokenStatus,
            CreatedAt,
            UpdatedAt);
        dbContext.PlatformAuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformAuthSessions.SingleAsync();
        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, saved.Status);
        Assert.Equal(UpdatedAt, saved.RevokedAt);
        Assert.Null(saved.IpAddress);
        Assert.Null(saved.UserAgent);
        Assert.Null(saved.DeviceName);
        Assert.Null(saved.LastSeenAt);
    }

    [Fact]
    public async Task Backfill_DoesNotSetRevokedAt_ForActiveSessions()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("active-session"),
            CreatedAt));
        await dbContext.SaveChangesAsync();

        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformAuthSessions.SingleAsync();
        Assert.Equal(PlatformAuthConstants.ActiveTokenStatus, saved.Status);
        Assert.Null(saved.RevokedAt);
    }

    [Fact]
    public async Task Backfill_RefreshToken_SetsPlatformUserIdFamilyAndTimestamps()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(
            sessionId,
            userId,
            CreateTokenHash("session-for-refresh"),
            CreatedAt));

        var usedToken = PlatformRefreshToken.CreateLegacy(
            Guid.NewGuid(),
            sessionId,
            CreateTokenHash("used-refresh"),
            PlatformAuthConstants.UsedTokenStatus,
            CreatedAt.AddDays(7),
            CreatedAt,
            UpdatedAt);

        var revokedToken = PlatformRefreshToken.CreateLegacy(
            Guid.NewGuid(),
            sessionId,
            CreateTokenHash("revoked-refresh"),
            PlatformAuthConstants.RevokedTokenStatus,
            CreatedAt.AddDays(7),
            CreatedAt,
            UpdatedAt);

        dbContext.PlatformRefreshTokens.AddRange(usedToken, revokedToken);
        await dbContext.SaveChangesAsync();

        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var savedUsed = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Id == usedToken.Id);
        Assert.Equal(userId, savedUsed.PlatformUserId);
        Assert.Equal(usedToken.Id, savedUsed.TokenFamilyId);
        Assert.Equal(UpdatedAt, savedUsed.UsedAt);
        Assert.Null(savedUsed.RevokedAt);

        var savedRevoked = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Id == revokedToken.Id);
        Assert.Equal(userId, savedRevoked.PlatformUserId);
        Assert.Equal(revokedToken.Id, savedRevoked.TokenFamilyId);
        Assert.Equal(UpdatedAt, savedRevoked.RevokedAt);
        Assert.Null(savedRevoked.UsedAt);
    }

    [Fact]
    public async Task Backfill_LoginAudit_MirrorsLegacyFields_WithoutInventingClientMetadata()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformLoginAudits.Add(PlatformLoginAudit.CreateLegacy(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.SuccessLoginResult,
            CreatedAt));
        await dbContext.SaveChangesAsync();

        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformLoginAudits.SingleAsync();
        Assert.Equal(CreatedAt, saved.AttemptedAt);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, saved.LoginStatus);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, saved.AuthenticationMethod);
        Assert.Null(saved.IpAddress);
        Assert.Null(saved.UserAgent);
        Assert.Null(saved.FailureReason);
        Assert.Null(saved.RiskScore);
        Assert.Null(saved.PlatformAuthSessionId);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, saved.LoginResult);
    }

    [Fact]
    public async Task Backfill_PasswordReset_SetsRequestedAt_WithoutInventingExpiresAt()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        dbContext.PlatformPasswordResetTokens.Add(PlatformPasswordResetToken.CreateLegacy(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("reset"),
            PlatformAuthConstants.PendingTokenStatus,
            CreatedAt));
        await dbContext.SaveChangesAsync();

        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.PlatformPasswordResetTokens.SingleAsync();
        Assert.Equal(CreatedAt, saved.RequestedAt);
        Assert.Null(saved.ExpiresAt);
        Assert.Null(saved.UsedAt);
        Assert.Null(saved.RevokedAt);
    }

    [Fact]
    public async Task Backfill_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SeedUser(dbContext, userId);

        var session = PlatformAuthSession.CreateLegacy(
            sessionId,
            userId,
            CreateTokenHash("idempotent-session"),
            PlatformAuthConstants.RevokedTokenStatus,
            CreatedAt,
            UpdatedAt);
        dbContext.PlatformAuthSessions.Add(session);

        var refreshToken = PlatformRefreshToken.CreateLegacy(
            Guid.NewGuid(),
            sessionId,
            CreateTokenHash("idempotent-refresh"),
            PlatformAuthConstants.UsedTokenStatus,
            CreatedAt.AddDays(7),
            CreatedAt,
            UpdatedAt);
        dbContext.PlatformRefreshTokens.Add(refreshToken);

        dbContext.PlatformLoginAudits.Add(PlatformLoginAudit.CreateLegacy(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.FailedLoginResult,
            CreatedAt));

        await dbContext.SaveChangesAsync();

        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);
        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        Assert.Equal(1, await dbContext.PlatformAuthSessions.CountAsync());
        Assert.Equal(1, await dbContext.PlatformRefreshTokens.CountAsync());
        Assert.Equal(1, await dbContext.PlatformLoginAudits.CountAsync());

        var savedSession = await dbContext.PlatformAuthSessions.SingleAsync();
        var savedRefresh = await dbContext.PlatformRefreshTokens.SingleAsync();
        var savedAudit = await dbContext.PlatformLoginAudits.SingleAsync();

        Assert.Equal(UpdatedAt, savedSession.RevokedAt);
        Assert.Equal(userId, savedRefresh.PlatformUserId);
        Assert.Equal(refreshToken.Id, savedRefresh.TokenFamilyId);
        Assert.Equal(UpdatedAt, savedRefresh.UsedAt);
        Assert.Equal(CreatedAt, savedAudit.AttemptedAt);
        Assert.Equal(PlatformAuthConstants.FailedLoginResult, savedAudit.LoginStatus);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, savedAudit.AuthenticationMethod);
    }

    [Fact]
    public async Task CompatibilityColumns_RemainPresentAfterAlignment()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessionHash = CreateTokenHash("compat-session");
        SeedUser(dbContext, userId);

        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(
            sessionId,
            userId,
            sessionHash,
            CreatedAt));

        dbContext.PlatformRefreshTokens.Add(PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionId,
            CreateTokenHash("compat-refresh"),
            CreatedAt.AddDays(7),
            CreatedAt));

        await dbContext.SaveChangesAsync();
        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        var refresh = await dbContext.PlatformRefreshTokens.SingleAsync();

        Assert.Equal(PlatformAuthConstants.ActiveTokenStatus, session.Status);
        Assert.Equal(sessionHash, session.SessionTokenHash);
        Assert.Equal(PlatformAuthConstants.ActiveTokenStatus, refresh.Status);
        Assert.False(string.IsNullOrWhiteSpace(refresh.TokenHash));
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
