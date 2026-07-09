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

public sealed class PlatformAuthRefreshServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_ReturnsNewAccessTokenAndRotatesRefreshToken()
    {
        var repository = RefreshRepository.CreateActive("refresh-token");
        var service = CreateService(repository);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("jwt-access-token", result.Value!.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        Assert.Contains(PlatformPermissionCodes.TenantsView, result.Value.Permissions);
        Assert.True(repository.RotateCalled);
        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, repository.StoredRefreshToken!.Status);
    }

    [Fact]
    public async Task RefreshAsync_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        var repository = RefreshRepository.CreateActive("refresh-token");
        var service = CreateService(repository);

        var result = await service.RefreshAsync("missing-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.invalid_refresh_token", result.Error.Code);
    }

    [Fact]
    public async Task RefreshAsync_WithUsedRefreshToken_RevokesSessionAndReturnsUnauthorized()
    {
        var repository = RefreshRepository.CreateUsed("refresh-token");
        var service = CreateService(repository);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.refresh_token_reused", result.Error.Code);
        Assert.NotNull(repository.RevokedSessionId);
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedSession_ReturnsUnauthorized()
    {
        var repository = RefreshRepository.CreateActive("refresh-token", sessionStatus: PlatformAuthConstants.RevokedTokenStatus);
        var service = CreateService(repository);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.invalid_session", result.Error.Code);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredRefreshToken_ReturnsUnauthorized()
    {
        var repository = RefreshRepository.CreateActive("refresh-token", expiresAt: Now.AddMinutes(-1));
        var service = CreateService(repository);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.invalid_session", result.Error.Code);
    }

    [Fact]
    public async Task RefreshAsync_WithInactiveUser_ReturnsForbidden()
    {
        var repository = RefreshRepository.CreateActive("refresh-token", userStatus: PlatformAuthConstants.InactiveStatus);
        var service = CreateService(repository);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.platform_access_denied", result.Error.Code);
        Assert.NotNull(repository.RevokedSessionId);
    }

    [Fact]
    public async Task RefreshAsync_WhenRotationFails_ReturnsUnauthorized()
    {
        var repository = RefreshRepository.CreateActive("refresh-token", rotationSucceeds: false);
        var service = CreateService(repository);

        var result = await service.RefreshAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_auth.invalid_refresh_token", result.Error.Code);
    }

    private static PlatformAuthService CreateService(RefreshRepository repository)
    {
        return new PlatformAuthService(
            repository,
            new PlatformAuthRequestValidator(),
            new FakePasswordHashService(),
            new FakeJwtTokenFactory(),
            new FakeRefreshTokenGenerator(),
            new FakeTokenHashService(),
            new FakeDateTimeProvider(),
            JwtSettings);
    }

    private sealed class RefreshRepository : IPlatformAuthRepository
    {
        private readonly PlatformAuthRefreshContext _context;
        private readonly bool _rotationSucceeds;

        private RefreshRepository(PlatformAuthRefreshContext context, bool rotationSucceeds)
        {
            _context = context;
            _rotationSucceeds = rotationSucceeds;
        }

        public PlatformRefreshToken? StoredRefreshToken { get; private set; }

        public bool RotateCalled { get; private set; }

        public Guid? RevokedSessionId { get; private set; }

        public static RefreshRepository CreateActive(
            string refreshToken,
            string sessionStatus = PlatformAuthConstants.ActiveTokenStatus,
            string userStatus = PlatformAuthConstants.ActiveStatus,
            DateTimeOffset? expiresAt = null,
            bool rotationSucceeds = true)
        {
            var userId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var user = PlatformUser.Create(userId, "admin@tmepos.test", "hash", userStatus, Now);
            var session = PlatformAuthSession.Create(sessionId, userId, "session-hash", Now);
            if (sessionStatus == PlatformAuthConstants.RevokedTokenStatus)
            {
                session.Revoke(Now);
            }

            var refreshTokenEntity = PlatformRefreshToken.Create(
                Guid.NewGuid(),
                sessionId,
                "hash:" + refreshToken,
                expiresAt ?? Now.AddDays(7),
                Now);

            return new RefreshRepository(
                new PlatformAuthRefreshContext
                {
                    RefreshToken = refreshTokenEntity,
                    Session = session,
                    User = user
                },
                rotationSucceeds);
        }

        public static RefreshRepository CreateUsed(string refreshToken)
        {
            var repository = CreateActive(refreshToken);
            repository._context.RefreshToken.MarkUsed(Now);
            return repository;
        }

        public Task<PlatformUser?> FindUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult<PlatformUser?>(null);

        public Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>([PlatformPermissionCodes.TenantsView]);

        public Task SaveFailedLoginAuditAsync(PlatformLoginAudit audit, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveFailedCredentialAttemptAsync(
            PlatformLoginAudit audit,
            DateTimeOffset failedAttemptWindowStart,
            int maxFailedAttempts,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveSuccessfulLoginAsync(
            PlatformAuthSession session,
            PlatformRefreshToken refreshToken,
            PlatformLoginAudit audit,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RevokeCurrentSessionAsync(
            Guid platformUserId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken,
            Guid? revokedByPlatformUserId = null,
            string? revokeReason = null)
        {
            RevokedSessionId = sessionId;
            return Task.CompletedTask;
        }

        public Task<PlatformAuthRefreshContext?> FindRefreshContextByTokenHashAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(refreshTokenHash == "hash:refresh-token" ? _context : null);
        }

        public Task<bool> TryRotateRefreshTokenAsync(
            Guid refreshTokenId,
            PlatformRefreshToken replacementRefreshToken,
            string replacementSessionTokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RotateCalled = true;
            StoredRefreshToken = _context.RefreshToken;
            StoredRefreshToken.MarkUsed(now);
            return Task.FromResult(_rotationSucceeds);
        }
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => "hash";

        public bool VerifyPassword(string password, string passwordHash) => true;
    }

    private sealed class FakeJwtTokenFactory : IJwtTokenFactory
    {
        public JwtTokenResult CreateAccessToken(JwtTokenDescriptor descriptor)
            => new("jwt-access-token", Now.AddMinutes(15));
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public RefreshTokenResult CreateRefreshToken(int lifetimeDays)
            => new("refresh-token", Now.AddDays(lifetimeDays));
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


