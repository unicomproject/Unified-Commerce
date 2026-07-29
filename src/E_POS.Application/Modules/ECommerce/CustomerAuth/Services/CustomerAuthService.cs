using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerAuthService : ICustomerAuthService
{
    private const int MaxFailedAttempts = 5;
    private const int VerificationOtpMaxAttempts = 5;
    private const int VerificationOtpMinutes = 15;
    private const int PasswordResetTokenMinutes = 60;
    private const string CustomerCodeSequenceKey = "CUSTOMER_CODE";
    private const string CustomerCodePrefix = "CUS";
    private const int CustomerCodePaddingLength = 6;

    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private static readonly ApplicationError InvalidCredentials =
        new("customer_auth.invalid_credentials", "Invalid email/phone or password.");
    private static readonly ApplicationError InvalidSession =
        new("customer_auth.invalid_session", "Invalid customer session.");
    private static readonly ApplicationError InvalidRefreshToken =
        new("customer_auth.invalid_refresh_token", "The refresh token is invalid or expired.");
    private static readonly ApplicationError TenantAccessDenied =
        new("customer_auth.tenant_access_denied", "Tenant access denied.");
    private static readonly ApplicationError EmailDeliveryUnavailable =
        new("customer_auth.email_delivery_unavailable", "Email delivery is not configured or could not send the message.");
    private static readonly ApplicationError DuplicateEmail =
        new("customer_auth.email_already_registered", "An account with this email address already exists.");
    private static readonly ApplicationError InvalidVerificationCode =
        new("customer_auth.invalid_verification_code", "The verification code is invalid or expired.");
    private static readonly ApplicationError InvalidResetToken =
        new("customer_auth.invalid_reset_token", "The password reset link is invalid or expired.");
    private static readonly ApplicationError EmailNotVerified =
        new("customer_auth.email_not_verified", "Please verify your email before signing in.");

    private readonly ICustomerAuthRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IApplicationEmailSender _emailSender;
    private readonly ICustomerPasswordResetLinkBuilder _passwordResetLinkBuilder;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly CustomerJwtSettings _jwtSettings;

    public CustomerAuthService(
        ICustomerAuthRepository repository,
        IPasswordHashService passwordHashService,
        IJwtTokenFactory jwtTokenFactory,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        IApplicationEmailSender emailSender,
        ICustomerPasswordResetLinkBuilder passwordResetLinkBuilder,
        ICodeSequenceRepository codeSequenceRepository,
        CustomerJwtSettings jwtSettings)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _jwtTokenFactory = jwtTokenFactory;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
        _emailSender = emailSender;
        _passwordResetLinkBuilder = passwordResetLinkBuilder;
        _codeSequenceRepository = codeSequenceRepository;
        _jwtSettings = jwtSettings;
    }
    public async Task<ApplicationResult> RegisterAsync(
        Guid tenantId,
        CustomerRegisterRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRegister(tenantId, request);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        if (!_emailSender.IsConfigured)
            return ApplicationResult.Failure(EmailDeliveryUnavailable);

        if (!await _repository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult.Failure(TenantAccessDenied);

        var email = NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var existingAccount = await _repository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: false,
            cancellationToken);

        if (existingAccount is not null)
        {
            if (existingAccount.Account.EmailVerifiedAt.HasValue)
                return ApplicationResult.Failure(DuplicateEmail);

            return await SendVerificationForExistingAccountAsync(
                existingAccount,
                ipAddress,
                userAgent,
                cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        var customerId = Guid.NewGuid();
        var customerCode = await _codeSequenceRepository.GetNextCodeAsync(
            tenantId,
            CustomerCodeSequenceKey,
            CustomerCodePrefix,
            CustomerCodePaddingLength,
            now,
            cancellationToken);
        var customer = CustomerEntity.CreateECommerceCustomer(
            customerId,
            tenantId,
            customerCode,
            email,
            request.FirstName,
            request.LastName,
            now);
        var account = CustomerAuthAccount.Create(
            Guid.NewGuid(),
            tenantId,
            customerId,
            _passwordHashService.HashPassword(request.Password),
            now);
        var (verificationOtp, rawCode) = CreateEmailVerificationOtp(
            tenantId,
            customerId,
            email,
            normalizedEmail,
            ipAddress,
            userAgent,
            now);
        var consents = CreateRegistrationConsents(
            tenantId,
            customerId,
            request.SendOffers,
            ipAddress,
            userAgent,
            now);

        var saved = await _repository.RegisterCustomerAsync(
            customer,
            account,
            verificationOtp,
            consents,
            cancellationToken);
        if (!saved)
            return ApplicationResult.Failure(DuplicateEmail);

        return await SendEmailVerificationMessageAsync(
            email,
            customer.Name,
            rawCode,
            verificationOtp.ExpiresAt,
            customerId,
            cancellationToken);
    }

    public async Task<ApplicationResult> VerifyEmailAsync(
        Guid tenantId,
        CustomerVerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateVerifyEmail(tenantId, request);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        var email = NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var context = await _repository.FindPendingEmailVerificationAsync(
            tenantId,
            normalizedEmail,
            cancellationToken);
        if (context is null)
            return ApplicationResult.Failure(InvalidVerificationCode);

        var now = _dateTimeProvider.UtcNow;
        if (!IsTenantActive(context.TenantStatus) || !IsCustomerActive(context.CustomerStatus))
            return ApplicationResult.Failure(TenantAccessDenied);

        if (!context.VerificationOtp.IsPending(now))
        {
            if (context.VerificationOtp.ExpiresAt <= now)
                context.VerificationOtp.MarkExpired(now);
            else
                context.VerificationOtp.Invalidate(now);

            await _repository.SaveEmailVerificationAsync(
                context.VerificationOtp,
                context.Account,
                cancellationToken);
            return ApplicationResult.Failure(InvalidVerificationCode);
        }

        var expectedHash = HashOtp(tenantId, normalizedEmail, "EMAIL_VERIFY", request.Code.Trim());
        if (!SecureEquals(context.VerificationOtp.OtpHash, expectedHash))
        {
            context.VerificationOtp.RecordFailedAttempt(now);
            await _repository.SaveEmailVerificationAsync(
                context.VerificationOtp,
                context.Account,
                cancellationToken);
            return ApplicationResult.Failure(InvalidVerificationCode);
        }

        context.VerificationOtp.MarkVerified(now);
        context.Account.MarkEmailVerified(now);
        await _repository.SaveEmailVerificationAsync(
            context.VerificationOtp,
            context.Account,
            cancellationToken);

        return ApplicationResult.Success();
    }
    public async Task<ApplicationResult> ResendEmailVerificationAsync(
        Guid tenantId,
        CustomerResendEmailVerificationRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEmailOnly(tenantId, request.Email);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        if (!_emailSender.IsConfigured)
            return ApplicationResult.Failure(EmailDeliveryUnavailable);

        if (!await _repository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult.Failure(TenantAccessDenied);

        var normalizedEmail = CustomerEntity.NormalizeEmail(request.Email)!;
        var account = await _repository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: false,
            cancellationToken);

        if (account is null || account.Account.EmailVerifiedAt.HasValue)
            return ApplicationResult.Success();

        return await SendVerificationForExistingAccountAsync(
            account,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<ApplicationResult> ForgotPasswordAsync(
        Guid tenantId,
        CustomerForgotPasswordRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEmailOnly(tenantId, request.Email);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        if (!_emailSender.IsConfigured)
            return ApplicationResult.Failure(EmailDeliveryUnavailable);

        if (!await _repository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult.Failure(TenantAccessDenied);

        var email = NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var account = await _repository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: false,
            cancellationToken);

        if (account is null ||
            !IsCustomerActive(account.CustomerStatus) ||
            string.Equals(account.Account.Status, "DELETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(account.Account.Status, "DISABLED", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Success();
        }

        var now = _dateTimeProvider.UtcNow;
        var rawToken = CreateSecureToken();
        var resetToken = CustomerPasswordResetToken.Create(
            Guid.NewGuid(),
            tenantId,
            account.Account.Id,
            _tokenHashService.HashToken(rawToken, _jwtSettings.SigningKey),
            now.AddMinutes(PasswordResetTokenMinutes),
            now,
            ipAddress,
            userAgent);

        await _repository.SavePasswordResetTokenAsync(
            resetToken,
            now,
            cancellationToken);

        var resetUrl = _passwordResetLinkBuilder.BuildResetUrl(email, rawToken);
        return await SendPasswordResetMessageAsync(
            email,
            account.DisplayName,
            resetUrl,
            resetToken.ExpiresAt,
            account.CustomerId,
            cancellationToken);
    }

    public async Task<ApplicationResult> ResetPasswordAsync(
        Guid tenantId,
        CustomerResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateResetPassword(tenantId, request);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        var email = NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var tokenHash = _tokenHashService.HashToken(request.Token.Trim(), _jwtSettings.SigningKey);
        var context = await _repository.FindActivePasswordResetAsync(
            tenantId,
            normalizedEmail,
            tokenHash,
            cancellationToken);
        if (context is null)
            return ApplicationResult.Failure(InvalidResetToken);

        var now = _dateTimeProvider.UtcNow;
        if (!IsTenantActive(context.TenantStatus) || !IsCustomerActive(context.CustomerStatus))
            return ApplicationResult.Failure(TenantAccessDenied);

        if (!context.ResetToken.IsActive(now))
            return ApplicationResult.Failure(InvalidResetToken);

        context.ResetToken.Use(now);
        context.Account.SetPasswordHash(_passwordHashService.HashPassword(request.NewPassword), now);
        context.Account.MarkEmailVerified(now);
        await _repository.SavePasswordResetAsync(
            context.ResetToken,
            context.Account,
            now,
            cancellationToken);

        return ApplicationResult.Success();
    }
    public async Task<ApplicationResult<CustomerAuthTokenResult>> LoginAsync(
        Guid tenantId,
        CustomerLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.EmailOrPhone) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.EmailOrPhone.Trim().Length > 150 ||
            request.Password.Length > 512 ||
            request.DeviceName?.Trim().Length > 150)
        {
            return ApplicationResult<CustomerAuthTokenResult>.Failure(
                new ApplicationError("customer_auth.validation_failed",
                    "Tenant, email/phone, and password are required."));
        }

        var identifier = request.EmailOrPhone.Trim();
        var isEmail = identifier.Contains('@', StringComparison.Ordinal);
        var account = await _repository.FindLoginAccountAsync(
            tenantId,
            isEmail ? CustomerEntity.NormalizeEmail(identifier) ?? string.Empty : string.Empty,
            isEmail ? string.Empty : CustomerEntity.NormalizePhone(identifier),
            cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (account is null || account.Account.IsLocked(now))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(InvalidCredentials);

        var accountStatusAllowed =
            string.Equals(account.Account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(account.Account.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) &&
             account.Account.LockedUntil.HasValue && account.Account.LockedUntil <= now);
        if (!accountStatusAllowed ||
            !string.Equals(account.CustomerStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(account.Account.PasswordHash))
        {
            return ApplicationResult<CustomerAuthTokenResult>.Failure(InvalidCredentials);
        }

        if (isEmail && !account.Account.EmailVerifiedAt.HasValue)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(EmailNotVerified);

        if (!_passwordHashService.VerifyPassword(request.Password, account.Account.PasswordHash))
        {
            account.Account.RecordFailedLogin(now, MaxFailedAttempts, LockDuration);
            await _repository.SaveFailedLoginAsync(account.Account, cancellationToken);
            return ApplicationResult<CustomerAuthTokenResult>.Failure(InvalidCredentials);
        }

        if (!IsTenantActive(account.TenantStatus))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(TenantAccessDenied);

        account.Account.RecordSuccessfulLogin(now);
        var sessionId = Guid.NewGuid();
        var accessToken = CreateAccessToken(account, sessionId);
        var refreshToken = _refreshTokenGenerator.CreateRefreshToken(_jwtSettings.RefreshTokenDays);
        var session = CustomerAuthSession.Create(
            sessionId,
            account.TenantId,
            account.Account.Id,
            _tokenHashService.HashToken(sessionId.ToString("N"), _jwtSettings.SigningKey),
            ipAddress,
            userAgent,
            request.DeviceName,
            refreshToken.ExpiresAt,
            now);
        var refreshTokenEntity = CustomerRefreshToken.Create(
            Guid.NewGuid(),
            account.TenantId,
            sessionId,
            _tokenHashService.HashToken(refreshToken.Token, _jwtSettings.SigningKey),
            Guid.NewGuid(),
            refreshToken.ExpiresAt,
            now);
        await _repository.SaveSuccessfulLoginAsync(
            account.Account,
            session,
            refreshTokenEntity,
            cancellationToken);

        return ApplicationResult<CustomerAuthTokenResult>.Success(
            CreateTokenResult(account, accessToken, refreshToken.Token, refreshToken.ExpiresAt, request.RememberMe));
    }

    public async Task<ApplicationResult<CustomerAuthTokenResult>> RefreshAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(refreshToken))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(InvalidRefreshToken);

        var now = _dateTimeProvider.UtcNow;
        var replacement = _refreshTokenGenerator.CreateRefreshToken(_jwtSettings.RefreshTokenDays);
        var rotation = await _repository.RotateRefreshTokenAsync(
            tenantId,
            _tokenHashService.HashToken(refreshToken, _jwtSettings.SigningKey),
            Guid.NewGuid(),
            _tokenHashService.HashToken(replacement.Token, _jwtSettings.SigningKey),
            replacement.ExpiresAt,
            now,
            cancellationToken);
        if (rotation.Status != CustomerRefreshRotationStatus.Succeeded ||
            rotation.Account is null ||
            !rotation.SessionId.HasValue)
        {
            return ApplicationResult<CustomerAuthTokenResult>.Failure(InvalidRefreshToken);
        }

        var accessToken = CreateAccessToken(rotation.Account, rotation.SessionId.Value);
        return ApplicationResult<CustomerAuthTokenResult>.Success(
            CreateTokenResult(
                rotation.Account,
                accessToken,
                replacement.Token,
                replacement.ExpiresAt,
                true));
    }

    public async Task<ApplicationResult> LogoutAsync(
        Guid tenantId,
        Guid customerId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty || sessionId == Guid.Empty)
            return ApplicationResult.Failure(InvalidSession);

        var revoked = await _repository.RevokeSessionAsync(
            tenantId, customerId, sessionId, _dateTimeProvider.UtcNow, cancellationToken);
        return revoked ? ApplicationResult.Success() : ApplicationResult.Failure(InvalidSession);
    }

    public async Task<ApplicationResult<CustomerProfileResponse>> GetProfileAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetCustomerByIdAsync(tenantId, customerId, cancellationToken);

        if (customer is null)
            return ApplicationResult<CustomerProfileResponse>.Failure(new ApplicationError("customer.not_found", "Customer not found."));

        return ApplicationResult<CustomerProfileResponse>.Success(new CustomerProfileResponse
        {
            FirstName = customer.FirstName ?? string.Empty,
            LastName = customer.LastName ?? string.Empty,
            Email = customer.Email ?? string.Empty,
            Phone = customer.Phone ?? string.Empty
        });
    }

    public async Task<ApplicationResult> UpdateProfileAsync(
        Guid tenantId,
        Guid customerId,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetCustomerByIdAsync(tenantId, customerId, cancellationToken);

        if (customer is null)
            return ApplicationResult.Failure(new ApplicationError("customer.not_found", "Customer not found."));

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return ApplicationResult.Failure(new ApplicationError("customer.invalid_first_name", "First name is required."));

        customer.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            _dateTimeProvider.UtcNow);

        await _repository.UpdateCustomerAsync(customer, cancellationToken);

        return ApplicationResult.Success();
    }
    private async Task<ApplicationResult> SendVerificationForExistingAccountAsync(
        CustomerLoginAccount account,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!IsTenantActive(account.TenantStatus) || !IsCustomerActive(account.CustomerStatus))
            return ApplicationResult.Failure(TenantAccessDenied);

        if (string.IsNullOrWhiteSpace(account.Email))
            return ApplicationResult.Failure(EmailDeliveryUnavailable);

        var now = _dateTimeProvider.UtcNow;
        var email = NormalizeEmailAddress(account.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var (verificationOtp, rawCode) = CreateEmailVerificationOtp(
            account.TenantId,
            account.CustomerId,
            email,
            normalizedEmail,
            ipAddress,
            userAgent,
            now);

        await _repository.SaveEmailVerificationOtpAsync(
            verificationOtp,
            now,
            cancellationToken);

        return await SendEmailVerificationMessageAsync(
            email,
            account.DisplayName,
            rawCode,
            verificationOtp.ExpiresAt,
            account.CustomerId,
            cancellationToken);
    }

    private (CustomerVerificationOtp VerificationOtp, string RawCode) CreateEmailVerificationOtp(
        Guid tenantId,
        Guid customerId,
        string email,
        string normalizedEmail,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        var rawCode = CreateNumericCode();
        return (
            CustomerVerificationOtp.Create(
                Guid.NewGuid(),
                tenantId,
                customerId,
                "EMAIL_VERIFY",
                "EMAIL",
                email,
                normalizedEmail,
                HashOtp(tenantId, normalizedEmail, "EMAIL_VERIFY", rawCode),
                VerificationOtpMaxAttempts,
                now,
                now.AddMinutes(VerificationOtpMinutes),
                ipAddress,
                userAgent),
            rawCode);
    }

    private IReadOnlyCollection<CustomerConsent> CreateRegistrationConsents(
        Guid tenantId,
        Guid customerId,
        bool sendOffers,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        var consents = new List<CustomerConsent>
        {
            CustomerConsent.Grant(Guid.NewGuid(), tenantId, customerId, "TERMS", null, null, "ECOMMERCE", ipAddress, userAgent, now),
            CustomerConsent.Grant(Guid.NewGuid(), tenantId, customerId, "PRIVACY", null, null, "ECOMMERCE", ipAddress, userAgent, now)
        };

        if (sendOffers)
        {
            consents.Add(CustomerConsent.Grant(
                Guid.NewGuid(),
                tenantId,
                customerId,
                "MARKETING_EMAIL",
                null,
                null,
                "ECOMMERCE",
                ipAddress,
                userAgent,
                now));
        }

        return consents;
    }

    private async Task<ApplicationResult> SendEmailVerificationMessageAsync(
        string email,
        string displayName,
        string rawCode,
        DateTimeOffset expiresAt,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? email : displayName);
        var safeCode = WebUtility.HtmlEncode(rawCode);
        var message = new ApplicationEmailMessage(
            email,
            "Verify your OneVerz email",
            $"<p>Hello {safeName},</p><p>Use this 6-digit code to verify your email:</p><p><strong style='font-size:24px;letter-spacing:4px'>{safeCode}</strong></p><p>This code expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.</p><p>If you did not create this account, you can ignore this email.</p>".Trim(),
            $"Your OneVerz verification code is {rawCode}. It expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.",
            correlationId.ToString("D"));

        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        return sendResult.IsSuccess
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(EmailDeliveryUnavailable);
    }

    private async Task<ApplicationResult> SendPasswordResetMessageAsync(
        string email,
        string displayName,
        string resetUrl,
        DateTimeOffset expiresAt,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? email : displayName);
        var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
        var message = new ApplicationEmailMessage(
            email,
            "Reset your OneVerz password",
            $"<p>Hello {safeName},</p><p>Use the secure link below to reset your password.</p><p><a href='{safeResetUrl}'>Reset password</a></p><p>This link expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.</p><p>If you did not request this, you can ignore this email.</p>".Trim(),
            $"Reset your OneVerz password: {resetUrl} This link expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.",
            correlationId.ToString("D"));

        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        return sendResult.IsSuccess
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(EmailDeliveryUnavailable);
    }
    private JwtTokenResult CreateAccessToken(
        CustomerLoginAccount account,
        Guid sessionId)
    {
        return _jwtTokenFactory.CreateAccessToken(new JwtTokenDescriptor(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            _jwtSettings.SigningKey,
            _jwtSettings.AccessTokenMinutes,
            new Dictionary<string, object>
            {
                ["sub"] = account.CustomerId.ToString(),
                ["tenant_id"] = account.TenantId.ToString(),
                ["session_id"] = sessionId.ToString(),
                ["auth_account_id"] = account.Account.Id.ToString(),
                ["identity_type"] = "customer",
                ["jti"] = Guid.NewGuid().ToString("N"),
                ["email"] = account.Email ?? string.Empty
            }));
    }

    private static CustomerAuthTokenResult CreateTokenResult(
        CustomerLoginAccount account,
        JwtTokenResult accessToken,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        bool rememberMe)
    {
        return new CustomerAuthTokenResult(
            new CustomerLoginResponse(
                accessToken.AccessToken,
                accessToken.ExpiresAt,
                new CustomerLoginCustomerDto(
                    account.CustomerId,
                    account.TenantId,
                    account.DisplayName,
                    account.Email,
                    account.Phone)),
            refreshToken,
            refreshTokenExpiresAt,
            rememberMe);
    }

    private string HashOtp(Guid tenantId, string normalizedEmail, string purpose, string rawCode) =>
        _tokenHashService.HashToken(
            $"{tenantId:N}:{normalizedEmail}:{purpose}:{rawCode}",
            _jwtSettings.SigningKey);

    private static ApplicationError? ValidateRegister(Guid tenantId, CustomerRegisterRequest request)
    {
        var emailError = ValidateEmailOnly(tenantId, request.Email);
        if (emailError is not null)
            return emailError;

        if (!request.AgreeTerms)
            return new ApplicationError("customer_auth.terms_required", "You must agree to the terms and privacy policy.");

        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
            return passwordError;

        if (request.FirstName?.Trim().Length > 100 || request.LastName?.Trim().Length > 100)
            return new ApplicationError("customer_auth.validation_failed", "Name fields cannot exceed 100 characters.");

        return null;
    }

    private static ApplicationError? ValidateVerifyEmail(Guid tenantId, CustomerVerifyEmailRequest request)
    {
        var emailError = ValidateEmailOnly(tenantId, request.Email);
        if (emailError is not null)
            return emailError;

        var code = request.Code?.Trim() ?? string.Empty;
        if (code.Length != 6 || code.Any(x => !char.IsDigit(x)))
            return new ApplicationError("customer_auth.validation_failed", "A 6-digit verification code is required.");

        return null;
    }

    private static ApplicationError? ValidateResetPassword(Guid tenantId, CustomerResetPasswordRequest request)
    {
        var emailError = ValidateEmailOnly(tenantId, request.Email);
        if (emailError is not null)
            return emailError;

        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Trim().Length > 512)
            return new ApplicationError("customer_auth.validation_failed", "A valid password reset token is required.");

        return ValidatePassword(request.NewPassword);
    }

    private static ApplicationError? ValidateEmailOnly(Guid tenantId, string? email)
    {
        if (tenantId == Guid.Empty)
            return new ApplicationError("customer_auth.validation_failed", "Tenant is required.");

        var normalized = NormalizeEmailAddress(email);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 150 || !MailAddress.TryCreate(normalized, out var parsed) ||
            !string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return new ApplicationError("customer_auth.validation_failed", "A valid email address is required.");
        }

        return null;
    }

    private static ApplicationError? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 512)
            return new ApplicationError("customer_auth.validation_failed", "Password must be between 8 and 512 characters.");

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return new ApplicationError("customer_auth.validation_failed", "Password must contain letters and numbers.");

        return null;
    }

    private static string NormalizeEmailAddress(string? email) => (email ?? string.Empty).Trim();

    private static bool IsTenantActive(string status) =>
        string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);

    private static bool IsCustomerActive(string status) =>
        string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);

    private static bool SecureEquals(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string CreateNumericCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    private static string CreateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}