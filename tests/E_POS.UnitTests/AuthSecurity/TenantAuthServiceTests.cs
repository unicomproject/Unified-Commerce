using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Services;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Xunit;

namespace E_POS.UnitTests.AuthSecurity;

public sealed class TenantAuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 10, 30, 0, TimeSpan.Zero);
    private static readonly TenantJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Tenant",
        "TEST_TENANT_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    [Fact]
    public async Task LoginAsync_WithValidTenantUser_CreatesSessionRefreshTokenAndSuccessAudit()
    {
        var account = CreateAccount(passwordHash: "valid-hash");
        var repository = new FakeTenantAuthRepository(account, ["tenant.dashboard.view"]);
        var jwtFactory = new FakeJwtTokenFactory();
        var service = CreateService(repository, jwtFactory: jwtFactory);

        var result = await service.LoginAsync(new TenantLoginRequest(" user@tenant.test ", "correct-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("tenant-jwt-access-token", result.Value!.AccessToken);
        Assert.Equal("tenant-refresh-token", result.Value.RefreshToken);
        Assert.Equal(account.TenantId, result.Value.User.TenantId);
        Assert.Contains("tenant.dashboard.view", result.Value.Permissions);
        Assert.NotNull(repository.SavedSession);
        Assert.NotNull(repository.SavedRefreshToken);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(TenantAuthConstants.SuccessLoginResult, repository.SavedAudit!.LoginStatus);
        Assert.Equal(account.TenantId, repository.SavedSession!.TenantId);
        Assert.Equal("hash:tenant-refresh-token", repository.SavedRefreshToken!.TokenHash);
        Assert.Equal(Now.AddDays(7), repository.SavedRefreshToken.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsGenericFailureAndWritesFailedAudit()
    {
        var account = CreateAccount(passwordHash: "valid-hash");
        var repository = new FakeTenantAuthRepository(account, ["tenant.dashboard.view"]);
        var service = CreateService(repository);

        var result = await service.LoginAsync(new TenantLoginRequest("user@tenant.test", "wrong-password"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_auth.invalid_credentials", result.Error.Code);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(TenantAuthConstants.FailedLoginResult, repository.SavedAudit!.LoginStatus);
        Assert.Null(repository.SavedSession);
        Assert.Null(repository.SavedRefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithSuspendedTenant_ReturnsAccessDeniedAfterCredentialValidation()
    {
        var account = CreateAccount(passwordHash: "valid-hash", tenantStatus: "suspended");
        var repository = new FakeTenantAuthRepository(account, ["tenant.dashboard.view"]);
        var service = CreateService(repository);

        var result = await service.LoginAsync(new TenantLoginRequest("user@tenant.test", "correct-password"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_auth.tenant_access_denied", result.Error.Code);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(TenantAuthConstants.FailedLoginResult, repository.SavedAudit!.LoginStatus);
        Assert.Null(repository.SavedSession);
        Assert.Null(repository.SavedRefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithLockedUser_ReturnsGenericFailureAndWritesLockedAudit()
    {
        var account = CreateAccount(passwordHash: "valid-hash", userStatus: TenantAuthConstants.LockedUserStatus);
        var repository = new FakeTenantAuthRepository(account, ["tenant.dashboard.view"]);
        var service = CreateService(repository);

        var result = await service.LoginAsync(new TenantLoginRequest("user@tenant.test", "correct-password"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_auth.invalid_credentials", result.Error.Code);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(TenantAuthConstants.LockedLoginResult, repository.SavedAudit!.LoginStatus);
    }

    [Fact]
    public async Task LogoutAsync_WithCurrentTenantSession_RevokesResolvedSession()
    {
        var repository = new FakeTenantAuthRepository(null, []);
        var service = CreateService(repository);
        var tenantUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var result = await service.LogoutAsync(tenantUserId, tenantId, sessionId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantUserId, repository.RevokedTenantUserId);
        Assert.Equal(tenantId, repository.RevokedTenantId);
        Assert.Equal(sessionId, repository.RevokedSessionId);
        Assert.Equal(Now, repository.RevokedAt);
    }

    [Fact]
    public async Task LogoutAsync_WithMissingSessionContext_ReturnsInvalidSession()
    {
        var repository = new FakeTenantAuthRepository(null, []);
        var service = CreateService(repository);

        var result = await service.LogoutAsync(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_auth.invalid_session", result.Error.Code);
        Assert.Null(repository.RevokedTenantUserId);
    }

    private static TenantLoginAccount CreateAccount(
        string? passwordHash,
        string userStatus = TenantAuthConstants.ActiveUserStatus,
        string tenantStatus = TenantAuthConstants.ActiveTenantStatus)
    {
        return new TenantLoginAccount(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USER@TENANT.TEST",
            passwordHash,
            userStatus,
            tenantStatus);
    }

    private static TenantAuthService CreateService(
        FakeTenantAuthRepository repository,
        IJwtTokenFactory? jwtFactory = null)
    {
        return new TenantAuthService(
            repository,
            new FakePasswordHashService(),
            jwtFactory ?? new FakeJwtTokenFactory(),
            new FakeRefreshTokenGenerator(),
            new FakeTokenHashService(),
            new FakeDateTimeProvider(),
            JwtSettings);
    }

    private sealed class FakeTenantAuthRepository : ITenantAuthRepository
    {
        private readonly TenantLoginAccount? _account;
        private readonly IReadOnlyList<string> _permissions;

        public FakeTenantAuthRepository(TenantLoginAccount? account, IReadOnlyList<string> permissions)
        {
            _account = account;
            _permissions = permissions;
        }

        public TenantAuthSession? SavedSession { get; private set; }

        public TenantRefreshToken? SavedRefreshToken { get; private set; }

        public TenantLoginAudit? SavedAudit { get; private set; }

        public Guid? RevokedTenantUserId { get; private set; }

        public Guid? RevokedTenantId { get; private set; }

        public Guid? RevokedSessionId { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public Task<TenantLoginAccount?> FindLoginAccountByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_account?.Email == normalizedEmail ? _account : null);
        }

        public Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(
            Guid tenantUserId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_permissions);
        }

        public Task SaveFailedLoginAuditAsync(TenantLoginAudit audit, CancellationToken cancellationToken)
        {
            SavedAudit = audit;
            return Task.CompletedTask;
        }

        public Task SaveFailedCredentialAttemptAsync(
            TenantLoginAudit audit,
            DateTimeOffset failedAttemptWindowStart,
            int maxFailedAttempts,
            CancellationToken cancellationToken)
        {
            SavedAudit = audit;
            return Task.CompletedTask;
        }

        public Task SaveSuccessfulLoginAsync(
            TenantAuthSession session,
            TenantRefreshToken refreshToken,
            TenantLoginAudit audit,
            CancellationToken cancellationToken)
        {
            SavedSession = session;
            SavedRefreshToken = refreshToken;
            SavedAudit = audit;
            return Task.CompletedTask;
        }

        public Task RevokeCurrentSessionAsync(
            Guid tenantUserId,
            Guid tenantId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RevokedTenantUserId = tenantUserId;
            RevokedTenantId = tenantId;
            RevokedSessionId = sessionId;
            RevokedAt = now;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => "valid-hash";

        public bool VerifyPassword(string password, string passwordHash)
        {
            return password == "correct-password" && passwordHash == "valid-hash";
        }
    }

    private sealed class FakeJwtTokenFactory : IJwtTokenFactory
    {
        public string? JwtId { get; private set; }

        public JwtTokenResult CreateAccessToken(JwtTokenDescriptor descriptor)
        {
            JwtId = descriptor.Claims["jti"].ToString();
            return new JwtTokenResult("tenant-jwt-access-token", Now.AddMinutes(15));
        }
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public RefreshTokenResult CreateRefreshToken(int lifetimeDays)
        {
            return new RefreshTokenResult("tenant-refresh-token", Now.AddDays(lifetimeDays));
        }
    }

    private sealed class FakeTokenHashService : ITokenHashService
    {
        public string HashToken(string token, string signingKey) => "hash:" + token;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}

