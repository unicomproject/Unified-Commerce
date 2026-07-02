using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Services;

public sealed class PlatformAuthService : IPlatformAuthService
{
    private const int MaxFailedCredentialAttempts = 5;
    private static readonly TimeSpan FailedCredentialWindow = TimeSpan.FromMinutes(15);

    // Coordinates platform admin credential validation, token creation, and login auditing.
    private static readonly ApplicationError InvalidCredentials = new(
        "platform_auth.invalid_credentials",
        "Invalid email or password.");

    private readonly IPlatformAuthRepository _repository;
    private readonly IPlatformAuthRequestValidator _requestValidator;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PlatformJwtSettings _jwtSettings;

    public PlatformAuthService(
        IPlatformAuthRepository repository,
        IPlatformAuthRequestValidator requestValidator,
        IPasswordHashService passwordHashService,
        IJwtTokenFactory jwtTokenFactory,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        PlatformJwtSettings jwtSettings)
    {
        _repository = repository;
        _requestValidator = requestValidator;
        _passwordHashService = passwordHashService;
        _jwtTokenFactory = jwtTokenFactory;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
        _jwtSettings = jwtSettings;
    }

    public async Task<ApplicationResult<PlatformAdminLoginResponse>> LoginAsync(
        PlatformAdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = _requestValidator.ValidateLogin(request);
        if (validationError is not null) return ApplicationResult<PlatformAdminLoginResponse>.Failure(validationError);

        var normalizedEmail = PlatformUser.NormalizeEmail(request.Email);
        var now = _dateTimeProvider.UtcNow;
        var user = await _repository.FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Return the same failure for missing users to avoid account enumeration.
            await SaveFailedAuditAsync(null, PlatformAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

        if (user.Status == PlatformAuthConstants.LockedStatus)
        {
            // Keep locked-account responses generic so attackers cannot identify valid accounts.
            await SaveFailedAuditAsync(user.Id, PlatformAuthConstants.LockedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

        if (user.Status != PlatformAuthConstants.ActiveStatus || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            await SaveFailedAuditAsync(user.Id, PlatformAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

        if (!_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Count only real bad-password attempts toward lockout, not inactive/access-denied states.
            await SaveFailedAuditAsync(user.Id, PlatformAuthConstants.FailedLoginResult, now, cancellationToken, applyCredentialLockout: true);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

        // Issue platform tokens only after at least one active platform permission is found.
        var permissions = await _repository.GetActivePermissionCodesAsync(user.Id, cancellationToken);
        if (permissions.Count == 0)
        {
            await SaveFailedAuditAsync(user.Id, PlatformAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
                "platform_auth.platform_access_denied",
                "Platform access denied."));
        }

        var sessionId = Guid.NewGuid();
        var jwtId = Guid.NewGuid().ToString("N");
        var accessToken = _jwtTokenFactory.CreateAccessToken(CreateTokenDescriptor(user, sessionId, jwtId, permissions));
        var refreshToken = _refreshTokenGenerator.CreateRefreshToken(_jwtSettings.RefreshTokenDays);

        // Persist only token identifiers and hashes, never raw access or refresh tokens.
        var session = PlatformAuthSession.Create(
            sessionId,
            user.Id,
            _tokenHashService.HashToken(jwtId, _jwtSettings.SigningKey),
            now);

        var refreshTokenEntity = PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionId,
            _tokenHashService.HashToken(refreshToken.Token, _jwtSettings.SigningKey),
            refreshToken.ExpiresAt,
            now);

        var audit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            user.Id,
            PlatformAuthConstants.SuccessLoginResult,
            now);

        await _repository.SaveSuccessfulLoginAsync(session, refreshTokenEntity, audit, cancellationToken);

        var response = new PlatformAdminLoginResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            new PlatformAdminUserDto(user.Id, user.Email, user.Status),
            permissions);

        return ApplicationResult<PlatformAdminLoginResponse>.Success(response);
    }

    public async Task<ApplicationResult> LogoutAsync(
        Guid platformUserId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var validationError = _requestValidator.ValidateLogout(platformUserId, sessionId);
        if (validationError is not null) return ApplicationResult.Failure(validationError);

        await _repository.RevokeCurrentSessionAsync(
            platformUserId,
            sessionId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ApplicationResult.Success();
    }

    private JwtTokenDescriptor CreateTokenDescriptor(
        PlatformUser user,
        Guid sessionId,
        string jwtId,
        IReadOnlyList<string> permissions)
    {
        // Platform JWTs carry identity, session, token id, and permission claims.
        return new JwtTokenDescriptor(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            _jwtSettings.SigningKey,
            _jwtSettings.AccessTokenMinutes,
            new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["identity_type"] = PlatformAuthConstants.IdentityType,
                ["session_id"] = sessionId.ToString(),
                ["jti"] = jwtId,
                ["permissions"] = permissions
            });
    }

    private Task SaveFailedAuditAsync(
        Guid? platformUserId,
        string loginResult,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        bool applyCredentialLockout = false)
    {
        var audit = PlatformLoginAudit.Create(Guid.NewGuid(), platformUserId, loginResult, now);

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