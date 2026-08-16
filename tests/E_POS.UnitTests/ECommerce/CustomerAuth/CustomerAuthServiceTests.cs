using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.Customer.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.Customer.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.Customer.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
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


    [Fact]
    public async Task GoogleLoginAsync_ExistingExternalAccount_CreatesSession()
    {
        var loginAccount = CreateLoginAccount();
        var externalAccount = CustomerExternalAuthAccount.Create(
            Guid.NewGuid(),
            TenantId,
            loginAccount.Account.Id,
            CustomerExternalAuthAccount.GoogleProviderCode,
            "google-sub-123",
            "customer@example.com",
            true,
            Now.AddDays(-1));
        var repository = new FakeRepository(loginAccount)
        {
            ExternalLoginAccount = new CustomerExternalLoginAccount(loginAccount, externalAccount)
        };
        var jwtFactory = new FakeJwtTokenFactory();
        var service = CreateService(
            repository,
            jwtFactory,
            new FakeGoogleIdentityVerifier(new GoogleIdentityResult(
                "google-sub-123",
                "customer@example.com",
                true,
                "Test",
                "Customer",
                "Test Customer")));

        var result = await service.GoogleLoginAsync(
            TenantId,
            new CustomerGoogleLoginRequest
            {
                IdToken = "google-id-token",
                DeviceName = "Chrome",
                RememberMe = true
            },
            IPAddress.Parse("192.0.2.50"),
            "browser-agent",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("customer-access-token", result.Value!.Response.AccessToken);
        Assert.Equal("customer-refresh-token", result.Value.RefreshToken);
        Assert.True(result.Value.RememberMe);
        Assert.NotNull(repository.SavedExternalAccount);
        Assert.Equal(Now, repository.SavedExternalAccount!.LastLoginAt);
        Assert.NotNull(repository.SavedSession);
        Assert.Equal("Chrome", repository.SavedSession!.DeviceName);
        Assert.Equal("google-sub-123", repository.ExternalProviderSubject);
        Assert.Equal(CustomerId.ToString(), jwtFactory.Claims!["sub"]);
    }

    [Fact]
    public async Task GoogleLoginAsync_NewCustomerRequiresTermsConsent()
    {
        var repository = new FakeRepository(null);
        var service = CreateService(
            repository,
            googleIdentityVerifier: new FakeGoogleIdentityVerifier(new GoogleIdentityResult(
                "google-sub-new",
                "new@example.com",
                true,
                "New",
                "Customer",
                "New Customer")));

        var result = await service.GoogleLoginAsync(
            TenantId,
            new CustomerGoogleLoginRequest { IdToken = "google-id-token" },
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_auth.terms_required", result.Error.Code);
        Assert.Null(repository.SavedExternalAccount);
        Assert.Null(repository.SavedSession);
    }

    [Fact]
    public async Task GoogleLoginAsync_NewCustomerCreatesExternalAccountAndSession()
    {
        var repository = new FakeRepository(null);
        var service = CreateService(
            repository,
            googleIdentityVerifier: new FakeGoogleIdentityVerifier(new GoogleIdentityResult(
                "google-sub-new",
                "new@example.com",
                true,
                "New",
                "Customer",
                "New Customer")));

        var result = await service.GoogleLoginAsync(
            TenantId,
            new CustomerGoogleLoginRequest
            {
                IdToken = "google-id-token",
                AgreeTerms = true,
                SendOffers = true
            },
            null,
            "browser-agent",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.SavedExternalAccount);
        Assert.Equal("GOOGLE", repository.SavedExternalAccount!.ProviderCode);
        Assert.Equal("google-sub-new", repository.SavedExternalAccount.ProviderSubject);
        Assert.Equal("new@example.com", repository.SavedExternalAccount.ProviderEmail);
        Assert.True(repository.SavedExternalAccount.ProviderEmailVerified);
        Assert.Equal(Now, repository.SavedExternalAccount.LastLoginAt);
        Assert.NotNull(repository.SavedSession);
        Assert.NotNull(repository.SavedRefreshToken);
    }

    [Fact]
    public async Task GoogleLoginAsync_UnverifiedGoogleEmailFails()
    {
        var repository = new FakeRepository(null);
        var service = CreateService(
            repository,
            googleIdentityVerifier: new FakeGoogleIdentityVerifier(new GoogleIdentityResult(
                "google-sub-new",
                "new@example.com",
                false,
                null,
                null,
                null)));

        var result = await service.GoogleLoginAsync(
            TenantId,
            new CustomerGoogleLoginRequest
            {
                IdToken = "google-id-token",
                AgreeTerms = true
            },
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_auth.google_email_not_verified", result.Error.Code);
        Assert.Null(repository.SavedSession);
    }
    private static CustomerLoginAccount CreateLoginAccount()
    {
        var account = CustomerAuthAccount.Create(
            Guid.NewGuid(), TenantId, CustomerId, "valid-hash", Now.AddDays(-1));
        account.MarkEmailVerified(Now.AddDays(-1));
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
        IJwtTokenFactory? jwtTokenFactory = null,
        IGoogleIdentityVerifier? googleIdentityVerifier = null)
    {
        var passwordHashService = new FakePasswordHashService();
        var jwtFactory = jwtTokenFactory ?? new FakeJwtTokenFactory();
        var refreshTokenGenerator = new FakeRefreshTokenGenerator();
        var tokenHashService = new FakeTokenHashService();
        var clock = new FakeClock();
        var emailSender = new FakeApplicationEmailSender();
        var passwordResetLinkBuilder = new FakePasswordResetLinkBuilder();
        var codeSequenceRepository = new FakeCodeSequenceRepository();
        var validator = new CustomerAuthValidator();
        var tokenFactory = new CustomerTokenFactory(
            jwtFactory,
            refreshTokenGenerator,
            tokenHashService,
            JwtSettings);
        var otpService = new CustomerOtpService(tokenHashService, JwtSettings);
        var emailService = new CustomerAuthEmailService(emailSender);
        var consentFactory = new CustomerConsentFactory();
        var emailVerificationService = new CustomerEmailVerificationService(
            repository,
            clock,
            validator,
            otpService,
            emailService);
        var registrationService = new CustomerRegistrationService(
            repository,
            passwordHashService,
            clock,
            codeSequenceRepository,
            validator,
            emailVerificationService,
            otpService,
            consentFactory,
            emailService);
        var passwordResetService = new CustomerPasswordResetService(
            repository,
            passwordHashService,
            tokenHashService,
            clock,
            passwordResetLinkBuilder,
            validator,
            emailService,
            tokenFactory,
            JwtSettings);
        var loginService = new CustomerLoginService(
            repository,
            passwordHashService,
            clock,
            validator,
            tokenFactory);
        var googleAuthService = new CustomerGoogleAuthService(
            repository,
            clock,
            codeSequenceRepository,
            validator,
            tokenFactory,
            consentFactory,
            googleIdentityVerifier);
        var sessionService = new CustomerSessionService(
            repository,
            refreshTokenGenerator,
            tokenHashService,
            clock,
            tokenFactory,
            JwtSettings);
        var profileService = new CustomerProfileService(repository, clock);

        return new CustomerAuthService(
            registrationService,
            emailVerificationService,
            passwordResetService,
            loginService,
            googleAuthService,
            sessionService,
            profileService);
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
        public CustomerExternalAuthAccount? SavedExternalAccount { get; private set; }
        public CustomerExternalLoginAccount? ExternalLoginAccount { get; init; }
        public string? ExternalProviderSubject { get; private set; }
        public bool ExternalSaveResult { get; init; } = true;
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

        public Task<bool> TenantIsActiveAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(tenantId == TenantId);

        public Task<bool> NormalizedEmailExistsAsync(
            Guid tenantId,
            string normalizedEmail,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<CustomerLoginAccount?> FindAccountByEmailAsync(
            Guid tenantId,
            string normalizedEmail,
            bool trackAccount,
            CancellationToken cancellationToken) =>
            Task.FromResult(tenantId == TenantId ? _loginAccount : null);

        public Task<bool> RegisterCustomerAsync(
            E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer customer,
            CustomerAuthAccount account,
            CustomerVerificationOtp verificationOtp,
            IReadOnlyCollection<CustomerConsent> consents,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task SaveEmailVerificationOtpAsync(
            CustomerVerificationOtp verificationOtp,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<CustomerEmailVerificationContext?> FindPendingEmailVerificationAsync(
            Guid tenantId,
            string normalizedEmail,
            CancellationToken cancellationToken) =>
            Task.FromResult<CustomerEmailVerificationContext?>(null);

        public Task SaveEmailVerificationAsync(
            CustomerVerificationOtp verificationOtp,
            CustomerAuthAccount account,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SavePasswordResetTokenAsync(
            CustomerPasswordResetToken resetToken,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<CustomerPasswordResetContext?> FindActivePasswordResetAsync(
            Guid tenantId,
            string normalizedEmail,
            string tokenHash,
            CancellationToken cancellationToken) =>
            Task.FromResult<CustomerPasswordResetContext?>(null);

        public Task SavePasswordResetAsync(
            CustomerPasswordResetToken resetToken,
            CustomerAuthAccount account,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<CustomerExternalLoginAccount?> FindExternalLoginAccountAsync(
            Guid tenantId,
            string providerCode,
            string providerSubject,
            bool trackAccount,
            bool trackExternalAccount,
            CancellationToken cancellationToken)
        {
            ExternalProviderSubject = providerSubject;
            return Task.FromResult(tenantId == TenantId ? ExternalLoginAccount : null);
        }

        public Task<bool> RegisterExternalCustomerAsync(
            E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer customer,
            CustomerAuthAccount account,
            CustomerExternalAuthAccount externalAccount,
            IReadOnlyCollection<CustomerConsent> consents,
            CustomerAuthSession session,
            CustomerRefreshToken refreshToken,
            CancellationToken cancellationToken)
        {
            SavedExternalAccount = externalAccount;
            SavedSession = session;
            SavedRefreshToken = refreshToken;
            return Task.FromResult(ExternalSaveResult);
        }

        public Task<bool> LinkExternalAccountAndSaveLoginAsync(
            CustomerAuthAccount account,
            CustomerExternalAuthAccount externalAccount,
            CustomerAuthSession session,
            CustomerRefreshToken refreshToken,
            CancellationToken cancellationToken)
        {
            SavedExternalAccount = externalAccount;
            SavedSession = session;
            SavedRefreshToken = refreshToken;
            return Task.FromResult(ExternalSaveResult);
        }

        public Task SaveSuccessfulExternalLoginAsync(
            CustomerAuthAccount account,
            CustomerExternalAuthAccount externalAccount,
            CustomerAuthSession session,
            CustomerRefreshToken refreshToken,
            CancellationToken cancellationToken)
        {
            SavedExternalAccount = externalAccount;
            SavedSession = session;
            SavedRefreshToken = refreshToken;
            return Task.CompletedTask;
        }
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

        public Task<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer?> GetCustomerByIdAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer?>(null);
        }

        public Task UpdateCustomerAsync(
            E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer customer,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeApplicationEmailSender : IApplicationEmailSender
    {
        public bool IsConfigured => true;

        public Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(
            ApplicationEmailMessage message,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ApplicationEmailSendResult>.Success(
                new ApplicationEmailSendResult("email-operation", "Accepted")));
    }

    private sealed class FakePasswordResetLinkBuilder : ICustomerPasswordResetLinkBuilder
    {
        public string BuildResetUrl(string email, string rawToken) =>
            $"https://store.example/reset-password?email={email}&token={rawToken}";
    }

    private sealed class FakeCodeSequenceRepository : ICodeSequenceRepository
    {
        public Task<string> GetNextCodeAsync(
            Guid tenantId,
            string sequenceKey,
            string prefix,
            int paddingLength,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult("CUS000001");
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


    private sealed class FakeGoogleIdentityVerifier : IGoogleIdentityVerifier
    {
        private readonly GoogleIdentityResult _identity;

        public FakeGoogleIdentityVerifier(GoogleIdentityResult identity)
        {
            _identity = identity;
        }

        public Task<ApplicationResult<GoogleIdentityResult>> VerifyAsync(
            string idToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<GoogleIdentityResult>.Success(_identity));
    }
    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}


