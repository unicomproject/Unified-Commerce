using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.UnitTests.TenantAuth;

public sealed class TenantAdminInvitationAcceptanceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Validate_UnknownToken_ReturnsInvalid()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        tokens.Hash = "h1";
        repo.ReadSnapshot = null;

        var result = await sut.ValidateSetupTokenAsync("raw", CancellationToken.None);

        Assert.False(result.Valid);
        Assert.False(result.Expired);
    }

    [Fact]
    public async Task Validate_Expired_ReturnsExpiredFlag()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        tokens.Hash = "h1";
        repo.ReadSnapshot = Snapshot(expiresAt: Now.AddMinutes(-1));

        var result = await sut.ValidateSetupTokenAsync("raw", CancellationToken.None);

        Assert.False(result.Valid);
        Assert.True(result.Expired);
    }

    [Fact]
    public async Task Validate_ValidInvite_ReturnsEmail()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        tokens.Hash = "h1";
        repo.ReadSnapshot = Snapshot(expiresAt: Now.AddHours(1));

        var result = await sut.ValidateSetupTokenAsync("raw", CancellationToken.None);

        Assert.True(result.Valid);
        Assert.Equal("admin@example.test", result.Email);
    }

    [Fact]
    public async Task SetupPassword_Mismatch_FailsWithoutClaim()
    {
        var sut = CreateSut(out var repo, out _, out _);
        var result = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("tok", "Password1", "Password2"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorPasswordMismatch, result.Error.Code);
        Assert.Equal(0, repo.ClaimCalls);
    }

    [Fact]
    public async Task SetupPassword_WeakPassword_Fails()
    {
        var sut = CreateSut(out var repo, out _, out _);
        var result = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("tok", "short", "short"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorPasswordInvalid, result.Error.Code);
        Assert.Equal(0, repo.ClaimCalls);
    }

    [Fact]
    public async Task SetupPassword_Valid_ActivatesAndAccepts()
    {
        var sut = CreateSut(out var repo, out var tokens, out var hasher);
        tokens.Hash = "h1";
        var claim = CreateClaim(expiresAt: Now.AddHours(2));
        repo.Claim = claim;

        var result = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("raw", "Password1", "Password1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserInviteConstants.StatusAccepted, claim.Invite.InviteStatus);
        Assert.Equal(TenantUserConstants.StatusActive, claim.User.AccountStatus);
        Assert.Equal("hashed:Password1", claim.User.EncryptedPassword);
        Assert.Equal(1, hasher.HashCalls);
    }

    [Fact]
    public async Task SetupPassword_AlreadyAccepted_Fails()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        tokens.Hash = "h1";
        var claim = CreateClaim(expiresAt: Now.AddHours(2));
        claim.Invite.MarkAccepted(claim.User.Id, Now.AddMinutes(-1));
        repo.Claim = claim;

        var result = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("raw", "Password1", "Password1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteUsed, result.Error.Code);
    }

    [Fact]
    public async Task SetupPassword_RevokedInvite_Fails()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        tokens.Hash = "revoked";
        var claim = CreateClaim(expiresAt: Now.AddHours(2));
        claim.Invite.Revoke(Now.AddMinutes(-1));
        repo.Claim = claim;

        var result = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("raw", "Password1", "Password1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteCancelled, result.Error.Code);
        Assert.Equal(TenantUserConstants.StatusInvited, claim.User.AccountStatus);
    }

    [Fact]
    public async Task SetupPassword_ExpiredInvite_Fails()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        tokens.Hash = "expired";
        var claim = CreateClaim(expiresAt: Now.AddMinutes(-1));
        repo.Claim = claim;

        var result = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("raw", "Password1", "Password1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteExpired, result.Error.Code);
        Assert.Equal(TenantUserConstants.StatusInvited, claim.User.AccountStatus);
    }

    [Fact]
    public async Task SetupPassword_ResentOldTokenFailsButNewTokenAccepts()
    {
        var sut = CreateSut(out var repo, out var tokens, out _);
        var oldClaim = CreateClaim(expiresAt: Now.AddHours(2));
        oldClaim.Invite.Revoke(Now.AddMinutes(-1));
        tokens.Hash = "old-token-hash";
        repo.Claim = oldClaim;

        var oldResult = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("old-token", "Password1", "Password1"),
            CancellationToken.None);

        var newClaim = CreateClaim(expiresAt: Now.AddHours(2));
        tokens.Hash = "new-token-hash";
        repo.Claim = newClaim;

        var newResult = await sut.SetupPasswordAsync(
            new SetupTenantAdminPasswordRequest("new-token", "Password1", "Password1"),
            CancellationToken.None);

        Assert.True(oldResult.IsFailure);
        Assert.Equal(TenantAdminInvitationAcceptanceService.ErrorInviteCancelled, oldResult.Error.Code);
        Assert.True(newResult.IsSuccess);
        Assert.Equal(UserInviteConstants.StatusAccepted, newClaim.Invite.InviteStatus);
        Assert.Equal(TenantUserConstants.StatusActive, newClaim.User.AccountStatus);
    }

    private static TenantAdminInvitationAcceptanceService CreateSut(
        out FakeRepo repo,
        out FakeTokens tokens,
        out FakeHasher hasher)
    {
        repo = new FakeRepo();
        tokens = new FakeTokens();
        hasher = new FakeHasher();
        var clock = new FakeClock(Now);
        return new TenantAdminInvitationAcceptanceService(
            repo,
            tokens,
            hasher,
            new PlatformPasswordPolicyValidator(),
            clock,
            NullLogger<TenantAdminInvitationAcceptanceService>.Instance);
    }

    private static TenantAdminInvitationAcceptanceSnapshot Snapshot(DateTimeOffset expiresAt) => new()
    {
        InviteId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        InviteStatus = UserInviteConstants.StatusSent,
        ExpiresAt = expiresAt,
        AcceptedAt = null,
        CancelledAt = null,
        InvitedEmail = "admin@example.test",
        NormalizedInvitedEmail = "ADMIN@EXAMPLE.TEST",
        TenantStatus = TenantStatusConstants.Active,
        TenantDisplayName = "Demo",
        TenantUserId = Guid.NewGuid(),
        TenantUserStatus = TenantUserConstants.StatusInvited
    };

    private static TenantAdminInvitationAcceptanceClaim CreateClaim(DateTimeOffset expiresAt)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invite = UserInvite.CreatePending(
            Guid.NewGuid(), tenantId, "admin@example.test", "ADMIN@EXAMPLE.TEST",
            null, null, "h1", expiresAt, Now);
        invite.MarkSent(Now);
        var user = TenantUser.CreatePendingInvite(userId, tenantId, "admin@example.test", "Admin", null, null, Now, "USR-2026-99401");
        var tenant = Tenant.Create(tenantId, "T1", "t1", "Demo", TenantStatusConstants.Active, "LKR", "Asia/Colombo", null, null, Now);
        return new TenantAdminInvitationAcceptanceClaim
        {
            Invite = invite,
            User = user,
            Tenant = tenant,
            Operation = null,
            SiblingOpenInvites = [invite]
        };
    }

    private sealed class FakeRepo : ITenantAdminInvitationAcceptanceRepository
    {
        public TenantAdminInvitationAcceptanceSnapshot? ReadSnapshot { get; set; }
        public TenantAdminInvitationAcceptanceClaim? Claim { get; set; }
        public int ClaimCalls { get; private set; }

        public Task<TenantAdminInvitationAcceptanceSnapshot?> GetByTokenHashForReadAsync(
            string inviteTokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(ReadSnapshot);

        public async Task<TResult> ExecuteClaimAsync<TResult>(
            string inviteTokenHash,
            Func<TenantAdminInvitationAcceptanceClaim?, CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken)
        {
            ClaimCalls++;
            return await action(Claim, cancellationToken);
        }
    }

    private sealed class FakeTokens : IInvitationTokenService
    {
        public string Hash { get; set; } = "hash";
        public string GenerateToken() => "raw";
        public string HashToken(string rawToken) => Hash;
    }

    private sealed class FakeHasher : IPasswordHashService
    {
        public int HashCalls { get; private set; }
        public string HashPassword(string password)
        {
            HashCalls++;
            return $"hashed:{password}";
        }

        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
