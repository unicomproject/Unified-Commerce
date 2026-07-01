using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Services;

public sealed class PlatformAuthService : IPlatformAuthService
{
    private static readonly ApplicationError InvalidCredentials = new(
        "platform_auth.invalid_credentials",
        "Invalid email or password.");

    private readonly IPlatformAuthRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformAuthService(
        IPlatformAuthRepository repository,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PlatformAdminLoginResponse>> LoginAsync(
        PlatformAdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(new ApplicationError(
                "platform_auth.validation_failed",
                "Email and password are required."));
        }

        var normalizedEmail = PlatformUser.NormalizeEmail(request.Email);
        var now = _dateTimeProvider.UtcNow;
        var user = await _repository.FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            await SaveFailedAuditAsync(null, PlatformAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

        if (user.Status == PlatformAuthConstants.LockedStatus)
        {
            await SaveFailedAuditAsync(user.Id, PlatformAuthConstants.LockedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

        if (user.Status != PlatformAuthConstants.ActiveStatus ||
            string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            await SaveFailedAuditAsync(user.Id, PlatformAuthConstants.FailedLoginResult, now, cancellationToken);
            return ApplicationResult<PlatformAdminLoginResponse>.Failure(InvalidCredentials);
        }

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
        var accessToken = _jwtTokenService.CreateAccessToken(user, sessionId, jwtId, permissions);
        var refreshToken = _refreshTokenService.CreateRefreshToken();

        var session = PlatformAuthSession.Create(
            sessionId,
            user.Id,
            _tokenHashService.HashToken(jwtId),
            now);

        var refreshTokenEntity = PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionId,
            _tokenHashService.HashToken(refreshToken.Token),
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

    private Task SaveFailedAuditAsync(
        Guid? platformUserId,
        string loginResult,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var audit = PlatformLoginAudit.Create(Guid.NewGuid(), platformUserId, loginResult, now);
        return _repository.SaveFailedLoginAuditAsync(audit, cancellationToken);
    }
}

