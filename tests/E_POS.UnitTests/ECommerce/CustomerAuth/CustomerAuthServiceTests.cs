using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerAuth;

public sealed class CustomerAuthServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly CustomerJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Customer",
        "TEST_CUSTOMER_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        30);

    [Fact]
    public async Task LoginAsync_ValidEmail_CreatesCustomerSessionAndClaims()
    {
        var repository = new FakeRepository(CreateLoginAccount());
        var jwtFactory = new FakeJwtTokenFactory();
        var service = CreateService(repository, jwtFactory);
        var ipAddress = IPAddress.Parse("192.0.2.10");

        var result = await service.LoginAsync(
            TenantId,
            new CustomerLoginRequest
            {
                EmailOrPhone = " Customer@Example.com ",
                Password = "correct-password",
                DeviceName = "Mobile"
            },
            ipAddress,
            "test-agent",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("customer-access-token", result.Value!.Response.AccessToken);
        Assert.Equal("customer-refresh-token", result.Value.RefreshToken);
        Assert.Equal(Now.AddDays(30), result.Value.RefreshTokenExpiresAt);
        Assert.Equal("CUSTOMER@EXAMPLE.COM", repository.NormalizedEmail);
        Assert.Equal(string.Empty, repository.NormalizedPhone);
        Assert.NotNull(repository.SavedSession);
        Assert.Equal(TenantId, repository.SavedSession!.TenantId);
        Assert.Equal(ipAddress, repository.SavedSession.IpAddress);
        Assert.Equal("Mobile", repository.SavedSession.DeviceName);
        Assert.Equal(Now.AddDays(30), repository.SavedSession.ExpiresAt);
        Assert.NotNull(repository.SavedRefreshToken);
        Assert.Equal("hash:customer-refresh-token", repository.SavedRefreshToken!.TokenHash);
        Assert.NotEqual(result.Value.RefreshToken, repository.SavedRefreshToken.TokenHash);
        Assert.Equal(CustomerId.ToString(), jwtFactory.Claims!["sub"]);
        Assert.Equal(TenantId.ToString(), jwtFactory.Claims["tenant_id"]);
        Assert.Equal("customer", jwtFactory.Claims["identity_type"]);
        Assert.Equal(Now, repository.Account!.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_PhoneIdentifier_NormalizesOnlyPhone()
    {
        var repository = new FakeRepository(CreateLoginAccount());
        var service = CreateService(repository);

        var result = await service.LoginAsync(
            TenantId,
            new CustomerLoginRequest
            {
                EmailOrPhone = "+94 77-123-4567",
                Password = "correct-password"
            },
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, repository.NormalizedEmail);
        Assert.Equal("+94771234567", repository.NormalizedPhone);
    }

    [Fact]
    public async Task LoginAsync_FiveInvalidPasswords_LocksAccount()
    {
        var repository = new FakeRepository(CreateLoginAccount());
        var service = CreateService(repository);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await service.LoginAsync(
                TenantId,
                new CustomerLoginRequest
                {
                    EmailOrPhone = "customer@example.com",
                    Password = "wrong-password"
                },
                null,
                null,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal("customer_auth.invalid_credentials", result.Error.Code);
        }

        Assert.Equal(5, repository.Account!.FailedLoginCount);
        Assert.Equal("LOCKED", repository.Account.Status);
        Assert.Equal(Now.AddMinutes(15), repository.Account.LockedUntil);
        Assert.Equal(5, repository.FailedSaveCount);
        Assert.Null(repository.SavedSession);
    }

    [Fact]
    public async Task LoginAsync_SuspendedTenantWithWrongPassword_DoesNotRevealTenantStatus()
    {
        var loginAccount = CreateLoginAccount() with { TenantStatus = "suspended" };
        var repository = new FakeRepository(loginAccount);
        var service = CreateService(repository);

        var result = await service.LoginAsync(
            TenantId,
            new CustomerLoginRequest
            {
                EmailOrPhone = "customer@example.com",
                Password = "wrong-password"
            },
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_auth.invalid_credentials", result.Error.Code);
        Assert.Equal(1, repository.FailedSaveCount);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesAndIssuesNewAccessToken()
    {
        var account = CreateLoginAccount();
        var sessionId = Guid.NewGuid();
        var repository = new FakeRepository(account)
        {
            RotationResult = new CustomerRefreshRotationResult(
                CustomerRefreshRotationStatus.Succeeded,
                account,
                sessionId)
        };
        var jwtFactory = new FakeJwtTokenFactory();
        var service = CreateService(repository, jwtFactory);

        var result = await service.RefreshAsync(
            TenantId,
            "current-refresh-token",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("customer-access-token", result.Value!.Response.AccessToken);
        Assert.Equal("customer-refresh-token", result.Value.RefreshToken);
        Assert.Equal("hash:current-refresh-token", repository.CurrentTokenHash);
        Assert.Equal("hash:customer-refresh-token", repository.ReplacementTokenHash);
        Assert.Equal(sessionId.ToString(), jwtFactory.Claims!["session_id"]);
    }

    [Fact]
    public async Task RefreshAsync_ReusedToken_ReturnsGenericRefreshFailure()
    {
        var repository = new FakeRepository(CreateLoginAccount())
        {
            RotationResult = new CustomerRefreshRotationResult(
                CustomerRefreshRotationStatus.Reused,
                null,
                null)
        };
        var service = CreateService(repository);

        var result = await service.RefreshAsync(
            TenantId,
            "reused-refresh-token",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_auth.invalid_refresh_token", result.Error.Code);
    }

    [Fact]
    public async Task LogoutAsync_ValidContext_RevokesOnlyCurrentSession()
    {
        var repository = new FakeRepository(CreateLoginAccount()) { RevokeResult = true };
        var service = CreateService(repository);
        var sessionId = Guid.NewGuid();

        var result = await service.LogoutAsync(
            TenantId, CustomerId, sessionId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, repository.RevokedTenantId);
        Assert.Equal(CustomerId, repository.RevokedCustomerId);
        Assert.Equal(sessionId, repository.RevokedSessionId);
        Assert.Equal(Now, repository.RevokedAt);
    }

    private static CustomerLoginAccount CreateLoginAccount()
    {
        var account = CustomerAuthAccount.Create(
            Guid.NewGuid(), TenantId, CustomerId, "valid-hash", Now.AddDays(-1));
        return new CustomerLoginAccount(
            account,
            CustomerId,
            TenantId,
            "Test Customer",
            "customer@example.com",
            "+94771234567",
            "ACTIVE",
            "active");
    }

    private static CustomerAuthService CreateService(
        FakeRepository repository,
        IJwtTokenFactory? jwtTokenFactory = null)
    {
        return new CustomerAuthService(
            repository,
            new FakePasswordHashService(),
            jwtTokenFactory ?? new FakeJwtTokenFactory(),
            new FakeRefreshTokenGenerator(),
            new FakeTokenHashService(),
            new FakeClock(),
            JwtSettings);
    }

    private sealed class FakeRepository : ICustomerAuthRepository
    {
        private readonly CustomerLoginAccount? _loginAccount;

        public FakeRepository(CustomerLoginAccount? loginAccount)
        {
            _loginAccount = loginAccount;
            Account = loginAccount?.Account;
        }

        public CustomerAuthAccount? Account { get; }
        public string? NormalizedEmail { get; private set; }
        public string? NormalizedPhone { get; private set; }
        public int FailedSaveCount { get; private set; }
        public CustomerAuthSession? SavedSession { get; private set; }
        public CustomerRefreshToken? SavedRefreshToken { get; private set; }
        public bool RevokeResult { get; init; }
        public CustomerRefreshRotationResult RotationResult { get; init; } = new(
            CustomerRefreshRotationStatus.Invalid,
            null,
            null);
        public string? CurrentTokenHash { get; private set; }
        public string? ReplacementTokenHash { get; private set; }
        public Guid? RevokedTenantId { get; private set; }
        public Guid? RevokedCustomerId { get; private set; }
        public Guid? RevokedSessionId { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }

        public Task<CustomerLoginAccount?> FindLoginAccountAsync(
            Guid tenantId,
            string normalizedEmail,
            string normalizedPhone,
            CancellationToken cancellationToken)
        {
            NormalizedEmail = normalizedEmail;
            NormalizedPhone = normalizedPhone;
            return Task.FromResult(tenantId == TenantId ? _loginAccount : null);
        }

        public Task SaveFailedLoginAsync(
            CustomerAuthAccount account,
            CancellationToken cancellationToken)
        {
            FailedSaveCount++;
            return Task.CompletedTask;
        }

        public Task SaveSuccessfulLoginAsync(
            CustomerAuthAccount account,
            CustomerAuthSession session,
            CustomerRefreshToken refreshToken,
            CancellationToken cancellationToken)
        {
            SavedSession = session;
            SavedRefreshToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<CustomerRefreshRotationResult> RotateRefreshTokenAsync(
            Guid tenantId,
            string currentTokenHash,
            Guid replacementTokenId,
            string replacementTokenHash,
            DateTimeOffset replacementExpiresAt,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CurrentTokenHash = currentTokenHash;
            ReplacementTokenHash = replacementTokenHash;
            return Task.FromResult(RotationResult);
        }

        public Task<bool> RevokeSessionAsync(
            Guid tenantId,
            Guid customerId,
            Guid sessionId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RevokedTenantId = tenantId;
            RevokedCustomerId = customerId;
            RevokedSessionId = sessionId;
            RevokedAt = now;
            return Task.FromResult(RevokeResult);
        }
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => "valid-hash";

        public bool VerifyPassword(string password, string passwordHash) =>
            password == "correct-password" && passwordHash == "valid-hash";
    }

    private sealed class FakeJwtTokenFactory : IJwtTokenFactory
    {
        public IReadOnlyDictionary<string, object>? Claims { get; private set; }

        public JwtTokenResult CreateAccessToken(JwtTokenDescriptor descriptor)
        {
            Claims = descriptor.Claims;
            return new JwtTokenResult("customer-access-token", Now.AddMinutes(15));
        }
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public RefreshTokenResult CreateRefreshToken(int lifetimeDays) =>
            new("customer-refresh-token", Now.AddDays(lifetimeDays));
    }

    private sealed class FakeTokenHashService : ITokenHashService
    {
        public string HashToken(string token, string signingKey) => "hash:" + token;
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
