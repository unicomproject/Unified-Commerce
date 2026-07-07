using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

namespace E_POS.Application.Modules.Tenant.TenantAuth.Services;

public sealed class TenantAuthService : ITenantAuthService
{
    private const int MaxFailedCredentialAttempts = 5;
    private static readonly TimeSpan FailedCredentialWindow = TimeSpan.FromMinutes(15);

    private static readonly ApplicationError InvalidCredentials = new(
        "tenant_auth.invalid_credentials",
        "Invalid email or password.");

    private static readonly ApplicationError InvalidSession = new(
        "tenant_auth.invalid_session",
        "Invalid tenant session.");

    private readonly ITenantAuthRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly TenantJwtSettings _jwtSettings;

    public TenantAuthService(
        ITenantAuthRepository repository,
        IPasswordHashService passwordHashService,
        IJwtTokenFactory jwtTokenFactory,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        TenantJwtSettings jwtSettings)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _jwtTokenFactory = jwtTokenFactory;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
        _jwtSettings = jwtSettings;
    }

    public async Task<ApplicationResult<TenantLoginResponse>> LoginAsync(
        TenantLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
                "tenant_auth.validation_failed",
                "Email and password are required."));
        }

        var normalizedEmail = TenantUser.NormalizeEmail(request.Email);
        var now = _dateTimeProvider.UtcNow;
        var account = await _repository.FindLoginAccountByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (account is null)
        {
            // Return the same failure for missing users to avoid account enumeration.
            await SaveFailedAuditAsync(null, null, TenantAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<TenantLoginResponse>.Failure(InvalidCredentials);
        }

        if (string.Equals(account.UserStatus, TenantAuthConstants.LockedUserStatus, StringComparison.OrdinalIgnoreCase))
        {
            // Locked tenant users still receive a generic login failure.
            await SaveFailedAuditAsync(account.TenantId, account.TenantUserId, TenantAuthConstants.LockedLoginResult, now, cancellationToken);
            return ApplicationResult<TenantLoginResponse>.Failure(InvalidCredentials);
        }

        if (!string.Equals(account.UserStatus, TenantAuthConstants.ActiveUserStatus, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(account.PasswordHash))
        {
            await SaveFailedAuditAsync(account.TenantId, account.TenantUserId, TenantAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<TenantLoginResponse>.Failure(InvalidCredentials);
        }

        if (!_passwordHashService.VerifyPassword(request.Password, account.PasswordHash))
        {
            // Count only real bad-password attempts toward lockout, not inactive/tenant-denied states.
            await SaveFailedAuditAsync(account.TenantId, account.TenantUserId, TenantAuthConstants.FailedLoginResult, now, cancellationToken, applyCredentialLockout: true);
            return ApplicationResult<TenantLoginResponse>.Failure(InvalidCredentials);
        }

        if (!TenantAuthConstants.IsTenantLoginStatusAllowed(account.TenantStatus))
        {
            // E.g., suspended or archived tenants
            await SaveFailedAuditAsync(account.TenantId, account.TenantUserId, TenantAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<TenantLoginResponse>.Failure(new ApplicationError(
                "tenant_auth.tenant_access_denied",
                "Tenant access denied."));
        }

        var permissions = await _repository.GetActivePermissionCodesAsync(
            account.TenantUserId,
            account.TenantId,
            cancellationToken);

        var sessionId = Guid.NewGuid();
        var jwtId = Guid.NewGuid().ToString("N");
        var accessToken = _jwtTokenFactory.CreateAccessToken(CreateTokenDescriptor(account, sessionId, jwtId, permissions));
        var refreshToken = _refreshTokenGenerator.CreateRefreshToken(_jwtSettings.RefreshTokenDays);

        // Persist only token identifiers and hashes, never raw access or refresh tokens.
        var session = TenantAuthSession.Create(
            sessionId,
            account.TenantId,
            account.TenantUserId,
            null, // ipAddress
            null, // userAgent
            now.AddMinutes(_jwtSettings.AccessTokenMinutes),
            now);

        var refreshTokenEntity = TenantRefreshToken.Create(
            Guid.NewGuid(),
            account.TenantId,
            sessionId,
            account.TenantUserId,
            _tokenHashService.HashToken(refreshToken.Token, _jwtSettings.SigningKey),
            Guid.NewGuid(), // tokenFamilyId
            refreshToken.ExpiresAt,
            now);

        var audit = TenantLoginAudit.Create(
            Guid.NewGuid(),
            account.TenantId,
            account.TenantUserId,
            sessionId,
            null, // posDeviceId
            account.Email, // attemptedIdentifier
            "PASSWORD", // authenticationMethod
            "SUCCESS", // loginStatus
            null,
            null,
            null,
            null,
            now);

        await _repository.SaveSuccessfulLoginAsync(session, refreshTokenEntity, audit, cancellationToken);

        var response = new TenantLoginResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            new TenantLoginUserDto(
                account.TenantUserId,
                account.TenantId,
                account.Email,
                account.UserStatus,
                account.TenantStatus),
            permissions);

        return ApplicationResult<TenantLoginResponse>.Success(response);
    }

    public async Task<ApplicationResult> LogoutAsync(
        Guid tenantUserId,
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (tenantUserId == Guid.Empty || tenantId == Guid.Empty || sessionId == Guid.Empty)
        {
            return ApplicationResult.Failure(InvalidSession);
        }

        await _repository.RevokeCurrentSessionAsync(
            tenantUserId,
            tenantId,
            sessionId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ApplicationResult.Success();
    }
    private JwtTokenDescriptor CreateTokenDescriptor(
        TenantLoginAccount account,
        Guid sessionId,
        string jwtId,
        IReadOnlyList<string> permissions)
    {
        // Tenant JWTs carry tenant identity, session, token id, and permission claims.
        return new JwtTokenDescriptor(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            _jwtSettings.SigningKey,
            _jwtSettings.AccessTokenMinutes,
            new Dictionary<string, object>
            {
                ["sub"] = account.TenantUserId.ToString(),
                ["tenant_id"] = account.TenantId.ToString(),
                ["email"] = account.Email,
                ["identity_type"] = TenantAuthConstants.IdentityType,
                ["session_id"] = sessionId.ToString(),
                ["jti"] = jwtId,
                ["permissions"] = permissions
            });
    }

    private Task SaveFailedAuditAsync(
        Guid? tenantId,
        Guid? tenantUserId,
        string loginResult,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        bool applyCredentialLockout = false)
    {
        var audit = TenantLoginAudit.Create(
            Guid.NewGuid(),
            tenantId,
            tenantUserId,
            null,
            null,
            "UNKNOWN",
            "PASSWORD",
            loginResult,
            null,
            null,
            null,
            null,
            now);

        if (!applyCredentialLockout)
        {
            return _repository.SaveFailedLoginAuditAsync(audit, cancellationToken);
        }

        return _repository.SaveFailedCredentialAttemptAsync(
            audit,
            now.Subtract(FailedCredentialWindow),
            MaxFailedCredentialAttempts,
            cancellationToken);
    }
}

