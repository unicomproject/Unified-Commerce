using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformAuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 4, 30, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    [Fact]
    public async Task LoginAsync_WithValidPlatformAdmin_CreatesSessionRefreshTokenAndSuccessAudit()
    {
        var user = PlatformUser.Create(Guid.NewGuid(), "admin@tmepos.test", "valid-hash", PlatformAuthConstants.ActiveStatus, Now);
        var repository = new FakePlatformAuthRepository(user, ["platform.users.manage"]);
        var jwtFactory = new FakeJwtTokenFactory();
        var service = CreateService(repository, jwtFactory: jwtFactory);

        var result = await service.LoginAsync(new PlatformAdminLoginRequest(" admin@tmepos.test ", "correct-password"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("jwt-access-token", result.Value!.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        Assert.Contains("platform.users.manage", result.Value.Permissions);
        Assert.NotNull(repository.SavedSession);
        Assert.NotNull(repository.SavedRefreshToken);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, repository.SavedAudit!.LoginResult);
        Assert.Equal("hash:" + jwtFactory.JwtId, repository.SavedSession!.SessionTokenHash);
        Assert.Equal("hash:refresh-token", repository.SavedRefreshToken!.TokenHash);
        Assert.Equal(Now.AddDays(7), repository.SavedRefreshToken.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsGenericFailureAndWritesFailedAudit()
    {
        var user = PlatformUser.Create(Guid.NewGuid(), "admin@tmepos.test", "valid-hash", PlatformAuthConstants.ActiveStatus, Now);
        var repository = new FakePlatformAuthRepository(user, ["platform.users.manage"]);
        var service = CreateService(repository);

        var result = await service.LoginAsync(new PlatformAdminLoginRequest("admin@tmepos.test", "wrong-password"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.invalid_credentials", result.Error.Code);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(PlatformAuthConstants.FailedLoginResult, repository.SavedAudit!.LoginResult);
        Assert.Null(repository.SavedSession);
        Assert.Null(repository.SavedRefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithoutPlatformPermissions_ReturnsAccessDeniedAndWritesFailedAudit()
    {
        var user = PlatformUser.Create(Guid.NewGuid(), "admin@tmepos.test", "valid-hash", PlatformAuthConstants.ActiveStatus, Now);
        var repository = new FakePlatformAuthRepository(user, []);
        var service = CreateService(repository);

        var result = await service.LoginAsync(new PlatformAdminLoginRequest("admin@tmepos.test", "correct-password"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.platform_access_denied", result.Error.Code);
        Assert.NotNull(repository.SavedAudit);
        Assert.Equal(PlatformAuthConstants.FailedLoginResult, repository.SavedAudit!.LoginResult);
    }

    [Fact]
    public async Task LogoutAsync_WithCurrentPlatformSession_RevokesResolvedSession()
    {
        var repository = new FakePlatformAuthRepository(null, []);
        var service = CreateService(repository);
        var platformUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var result = await service.LogoutAsync(platformUserId, sessionId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(platformUserId, repository.RevokedPlatformUserId);
        Assert.Equal(sessionId, repository.RevokedSessionId);
        Assert.Equal(Now, repository.RevokedAt);
    }

    [Fact]
    public async Task LogoutAsync_WithMissingSessionContext_ReturnsInvalidSession()
    {
        var repository = new FakePlatformAuthRepository(null, []);
        var service = CreateService(repository);

        var result = await service.LogoutAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.invalid_session", result.Error.Code);
        Assert.Null(repository.RevokedPlatformUserId);
    }

    private static PlatformAuthService CreateService(
        FakePlatformAuthRepository repository,
        IJwtTokenFactory? jwtFactory = null)
    {
        return new PlatformAuthService(
            repository,
            new PlatformAuthRequestValidator(),
            new FakePasswordHashService(),
            jwtFactory ?? new FakeJwtTokenFactory(),
            new FakeRefreshTokenGenerator(),
            new FakeTokenHashService(),
            new FakeDateTimeProvider(),
            JwtSettings);
    }

    private sealed class FakePlatformAuthRepository : IPlatformAuthRepository
    {
        private readonly PlatformUser? _user;
        private readonly IReadOnlyList<string> _permissions;

        public FakePlatformAuthRepository(PlatformUser? user, IReadOnlyList<string> permissions)
        {
            _user = user;
            _permissions = permissions;
        }

        public PlatformAuthSession? SavedSession { get; private set; }

        public PlatformRefreshToken? SavedRefreshToken { get; private set; }

        public PlatformLoginAudit? SavedAudit { get; private set; }

        public Guid? RevokedPlatformUserId { get; private set; }

        public Guid? RevokedSessionId { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public Task<PlatformUser?> FindUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user?.NormalizedEmail == normalizedEmail ? _user : null);
        }

        public Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_permissions);
        }

        public Task SaveFailedLoginAuditAsync(PlatformLoginAudit audit, CancellationToken cancellationToken)
        {
            SavedAudit = audit;
            return Task.CompletedTask;
        }

        public Task SaveFailedCredentialAttemptAsync(
            PlatformLoginAudit audit,
            DateTimeOffset failedAttemptWindowStart,
            int maxFailedAttempts,
            CancellationToken cancellationToken)
        {
            SavedAudit = audit;
            return Task.CompletedTask;
        }

        public Task SaveSuccessfulLoginAsync(
            PlatformAuthSession session,
            PlatformRefreshToken refreshToken,
            PlatformLoginAudit audit,
            CancellationToken cancellationToken)
        {
            SavedSession = session;
            SavedRefreshToken = refreshToken;
            SavedAudit = audit;
            return Task.CompletedTask;
        }

        public Task RevokeCurrentSessionAsync(
            Guid platformUserId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken,
            Guid? revokedByPlatformUserId = null,
            string? revokeReason = null)
        {
            RevokedPlatformUserId = platformUserId;
            RevokedSessionId = sessionId;
            RevokedAt = now;
            return Task.CompletedTask;
        }

        public Task<PlatformAuthRefreshContext?> FindRefreshContextByTokenHashAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken)
            => Task.FromResult<PlatformAuthRefreshContext?>(null);

        public Task<bool> TryRotateRefreshTokenAsync(
            Guid refreshTokenId,
            PlatformRefreshToken replacementRefreshToken,
            string replacementSessionTokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
            => Task.FromResult(false);
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
            return new JwtTokenResult("jwt-access-token", Now.AddMinutes(15));
        }
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public RefreshTokenResult CreateRefreshToken(int lifetimeDays)
        {
            return new RefreshTokenResult("refresh-token", Now.AddDays(lifetimeDays));
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

