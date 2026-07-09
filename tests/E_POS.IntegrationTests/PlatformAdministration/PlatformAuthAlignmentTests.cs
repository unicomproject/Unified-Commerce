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
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

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

        usedToken.ApplyAlignmentBackfill(userId);
        revokedToken.ApplyAlignmentBackfill(userId);

        Assert.Equal(userId, usedToken.PlatformUserId);
        Assert.Equal(usedToken.Id, usedToken.TokenFamilyId);
        Assert.Equal(UpdatedAt, usedToken.UsedAt);
        Assert.Null(usedToken.RevokedAt);

        Assert.Equal(userId, revokedToken.PlatformUserId);
        Assert.Equal(revokedToken.Id, revokedToken.TokenFamilyId);
        Assert.Equal(UpdatedAt, revokedToken.RevokedAt);
        Assert.Null(revokedToken.UsedAt);
    }

    [Fact]
    public async Task Backfill_LoginAudit_MirrorsLegacyFields_WithoutInventingClientMetadata()
    {
        var userId = Guid.NewGuid();
        var audit = PlatformLoginAudit.CreateLegacy(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.SuccessLoginResult,
            CreatedAt);

        audit.ApplyAlignmentBackfill();

        Assert.Equal(CreatedAt, audit.AttemptedAt);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, audit.LoginStatus);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, audit.AuthenticationMethod);
        Assert.Null(audit.IpAddress);
        Assert.Null(audit.UserAgent);
        Assert.Null(audit.FailureReason);
        Assert.Null(audit.RiskScore);
        Assert.Null(audit.PlatformAuthSessionId);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, audit.LoginResult);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Backfill_PasswordReset_SetsRequestedAt_WithoutInventingExpiresAt()
    {
        var userId = Guid.NewGuid();
        var token = PlatformPasswordResetToken.CreateLegacy(
            Guid.NewGuid(),
            userId,
            CreateTokenHash("reset"),
            PlatformAuthConstants.PendingTokenStatus,
            CreatedAt);

        token.ApplyAlignmentBackfill();

        Assert.Equal(CreatedAt, token.RequestedAt);
        Assert.Null(token.ExpiresAt);
        Assert.Null(token.UsedAt);
        Assert.Null(token.RevokedAt);
        await Task.CompletedTask;
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
        refreshToken.ApplyAlignmentBackfill(userId);
        dbContext.PlatformRefreshTokens.Add(refreshToken);

        var audit = PlatformLoginAudit.CreateLegacy(
            Guid.NewGuid(),
            userId,
            PlatformAuthConstants.FailedLoginResult,
            CreatedAt);
        audit.ApplyAlignmentBackfill();
        dbContext.PlatformLoginAudits.Add(audit);

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
            CreatedAt,
            platformUserId: userId));

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
