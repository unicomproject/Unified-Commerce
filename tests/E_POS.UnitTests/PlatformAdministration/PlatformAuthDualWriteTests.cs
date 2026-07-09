using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

/// <summary>
/// Phase 8B: domain dual-write primitives for platform auth entities.
/// Verifies legacy status columns and Second Brain timestamp columns are written together.
/// </summary>
public sealed class PlatformAuthDualWriteTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 6, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Session_Revoke_SetsLegacyStatusAndRevokedAt()
    {
        var revokedBy = Guid.NewGuid();
        var session = PlatformAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "session-hash", CreatedAt);

        session.Revoke(Now, revokedBy, PlatformAuthAlignmentConstants.RevokeReason.Logout);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
        Assert.Equal(Now, session.RevokedAt);
        Assert.Equal(revokedBy, session.RevokedByPlatformUserId);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.Logout, session.RevokeReason);
        Assert.Equal(Now, session.UpdatedAt);
    }

    [Fact]
    public void Session_Revoke_WithoutActor_SetsStatusAndRevokedAtOnly()
    {
        var session = PlatformAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "session-hash", CreatedAt);

        session.Revoke(Now);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
        Assert.Equal(Now, session.RevokedAt);
        Assert.Null(session.RevokedByPlatformUserId);
        Assert.Null(session.RevokeReason);
    }

    [Fact]
    public void Session_Revoke_IsIdempotent()
    {
        var session = PlatformAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "session-hash", CreatedAt);
        session.Revoke(Now);

        session.Revoke(Now.AddMinutes(5), Guid.NewGuid(), "LATER");

        Assert.Equal(Now, session.RevokedAt);
        Assert.Null(session.RevokedByPlatformUserId);
        Assert.Null(session.RevokeReason);
    }

    [Fact]
    public void Session_TouchLastSeen_SetsLastSeenAt()
    {
        var session = PlatformAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "session-hash", CreatedAt);

        session.TouchLastSeen(Now);

        Assert.Equal(Now, session.LastSeenAt);
        Assert.Equal(Now, session.UpdatedAt);
        Assert.Equal(PlatformAuthConstants.ActiveTokenStatus, session.Status);
    }

    [Fact]
    public void Session_RotateSessionToken_ReplacesHash()
    {
        var session = PlatformAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "old-hash", CreatedAt);

        session.RotateSessionToken("new-hash", Now);

        Assert.Equal("new-hash", session.SessionTokenHash);
        Assert.Equal(Now, session.UpdatedAt);
    }

    [Fact]
    public void RefreshToken_MarkUsed_SetsLegacyStatusAndUsedAt()
    {
        var token = PlatformRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "refresh-hash", CreatedAt.AddDays(7), CreatedAt);

        token.MarkUsed(Now);

        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, token.Status);
        Assert.Equal(Now, token.UsedAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void RefreshToken_Revoke_SetsLegacyStatusRevokedAtAndReason()
    {
        var revokedBy = Guid.NewGuid();
        var token = PlatformRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "refresh-hash", CreatedAt.AddDays(7), CreatedAt);

        token.Revoke(Now, revokedBy, PlatformAuthAlignmentConstants.RevokeReason.RefreshTokenReuse);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, token.Status);
        Assert.Equal(Now, token.RevokedAt);
        Assert.Equal(revokedBy, token.RevokedByPlatformUserId);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.RefreshTokenReuse, token.RevokeReason);
        Assert.Null(token.UsedAt);
    }

    [Fact]
    public void RefreshToken_LinkReplacement_SetsReplacedByTokenId()
    {
        var replacementId = Guid.NewGuid();
        var token = PlatformRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "refresh-hash", CreatedAt.AddDays(7), CreatedAt);

        token.LinkReplacement(replacementId, Now);

        Assert.Equal(replacementId, token.ReplacedByTokenId);
        Assert.Equal(Now, token.UpdatedAt);
    }

    [Fact]
    public void RefreshToken_Create_SupportsPlatformUserIdAndTokenFamilyId()
    {
        var platformUserId = Guid.NewGuid();
        var tokenFamilyId = Guid.NewGuid();

        var token = PlatformRefreshToken.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "refresh-hash",
            CreatedAt.AddDays(7),
            CreatedAt,
            platformUserId,
            tokenFamilyId);

        Assert.Equal(platformUserId, token.PlatformUserId);
        Assert.Equal(tokenFamilyId, token.TokenFamilyId);
        Assert.Equal(PlatformAuthConstants.ActiveTokenStatus, token.Status);
    }

    [Fact]
    public void RefreshToken_Create_WithoutAlignmentArguments_LeavesNewColumnsNull()
    {
        var token = PlatformRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "refresh-hash", CreatedAt.AddDays(7), CreatedAt);

        Assert.Null(token.PlatformUserId);
        Assert.Null(token.TokenFamilyId);
        Assert.Null(token.ReplacedByTokenId);
    }

    [Fact]
    public void LoginAudit_Create_DualWritesLoginResultAndLoginStatus()
    {
        var audit = PlatformLoginAudit.Create(Guid.NewGuid(), Guid.NewGuid(), PlatformAuthConstants.SuccessLoginResult, Now);

        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, audit.LoginResult);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, audit.LoginStatus);
    }

    [Fact]
    public void LoginAudit_Create_MirrorsCreatedAtIntoAttemptedAt_WhenNotProvided()
    {
        var audit = PlatformLoginAudit.Create(Guid.NewGuid(), Guid.NewGuid(), PlatformAuthConstants.FailedLoginResult, Now);

        Assert.Equal(Now, audit.CreatedAt);
        Assert.Equal(Now, audit.AttemptedAt);
    }

    [Fact]
    public void LoginAudit_Create_UsesExplicitAttemptedAt_WhenProvided()
    {
        var attemptedAt = Now.AddSeconds(-2);

        var audit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlatformAuthConstants.SuccessLoginResult,
            Now,
            attemptedAt: attemptedAt);

        Assert.Equal(attemptedAt, audit.AttemptedAt);
        Assert.Equal(Now, audit.CreatedAt);
    }

    [Fact]
    public void LoginAudit_Create_DefaultsAuthenticationMethodToPassword()
    {
        var audit = PlatformLoginAudit.Create(Guid.NewGuid(), Guid.NewGuid(), PlatformAuthConstants.SuccessLoginResult, Now);

        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, audit.AuthenticationMethod);
    }

    [Fact]
    public void PasswordReset_CreatePending_WritesStatusRequestedAtAndExpiresAt()
    {
        var expiresAt = Now.AddHours(1);

        var token = PlatformPasswordResetToken.CreatePending(Guid.NewGuid(), Guid.NewGuid(), "reset-hash", expiresAt, Now);

        Assert.Equal(PlatformAuthConstants.PendingTokenStatus, token.Status);
        Assert.Equal(Now, token.RequestedAt);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Null(token.UsedAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void PasswordReset_MarkUsed_DualWritesStatusAndUsedAt()
    {
        var token = PlatformPasswordResetToken.CreatePending(Guid.NewGuid(), Guid.NewGuid(), "reset-hash", Now.AddHours(1), CreatedAt);

        token.MarkUsed(Now);

        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, token.Status);
        Assert.Equal(Now, token.UsedAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void PasswordReset_Revoke_DualWritesStatusAndRevokedAt()
    {
        var token = PlatformPasswordResetToken.CreatePending(Guid.NewGuid(), Guid.NewGuid(), "reset-hash", Now.AddHours(1), CreatedAt);

        token.Revoke(Now);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, token.Status);
        Assert.Equal(Now, token.RevokedAt);
        Assert.Null(token.UsedAt);
    }
}
