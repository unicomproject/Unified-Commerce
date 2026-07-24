using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerAuthService : ICustomerAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private static readonly ApplicationError InvalidCredentials =
        new("customer_auth.invalid_credentials", "Invalid email/phone or password.");
    private static readonly ApplicationError InvalidSession =
        new("customer_auth.invalid_session", "Invalid customer session.");
    private static readonly ApplicationError InvalidRefreshToken =
        new("customer_auth.invalid_refresh_token", "The refresh token is invalid or expired.");

    private readonly ICustomerAuthRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CustomerJwtSettings _jwtSettings;

    public CustomerAuthService(
        ICustomerAuthRepository repository,
        IPasswordHashService passwordHashService,
        IJwtTokenFactory jwtTokenFactory,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        CustomerJwtSettings jwtSettings)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _jwtTokenFactory = jwtTokenFactory;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
        _jwtSettings = jwtSettings;
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

        if (!_passwordHashService.VerifyPassword(request.Password, account.Account.PasswordHash))
        {
            account.Account.RecordFailedLogin(now, MaxFailedAttempts, LockDuration);
            await _repository.SaveFailedLoginAsync(account.Account, cancellationToken);
            return ApplicationResult<CustomerAuthTokenResult>.Failure(InvalidCredentials);
        }

        if (!string.Equals(account.TenantStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<CustomerAuthTokenResult>.Failure(
                new ApplicationError("customer_auth.tenant_access_denied", "Tenant access denied."));
        }

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
            CreateTokenResult(account, accessToken, refreshToken.Token, refreshToken.ExpiresAt));
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
                replacement.ExpiresAt));
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

        // Basic validation
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
        DateTimeOffset refreshTokenExpiresAt)
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
            refreshTokenExpiresAt);
    }
}
