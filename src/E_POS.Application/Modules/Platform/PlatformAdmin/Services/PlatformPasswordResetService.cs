using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformPasswordResetService : IPlatformPasswordResetService
{
    private static readonly ApplicationError UserNotFound = new(
        "platform_password_reset.user_not_found",
        "Platform user was not found.");

    private static readonly ApplicationError AccessDenied = new(
        "platform_users.access_denied",
        "Platform user access denied.");

    private static readonly ApplicationError InvalidUserState = new(
        "platform_password_reset.invalid_user_state",
        "Password reset cannot be initiated for this platform user.");

    private static readonly ApplicationError InvalidToken = new(
        "platform_password_reset.invalid_token",
        "Password reset token is invalid.");

    private static readonly ApplicationError TokenUsed = new(
        "platform_password_reset.token_used",
        "Password reset token has already been used.");

    private static readonly ApplicationError TokenRevoked = new(
        "platform_password_reset.token_revoked",
        "Password reset token has been revoked.");

    private static readonly ApplicationError TokenExpired = new(
        "platform_password_reset.token_expired",
        "Password reset token has expired.");

    private static readonly ApplicationError PasswordMismatch = new(
        "platform_password_reset.password_mismatch",
        "New password and confirmation do not match.");

    private readonly IPlatformPasswordResetRepository _repository;
    private readonly IPlatformUserRepository _userRepository;
    private readonly IPlatformAuthRepository _authRepository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPlatformPasswordPolicyValidator _passwordPolicyValidator;
    private readonly IPlatformPasswordResetLinkBuilder _linkBuilder;
    private readonly IPlatformPasswordResetDeliveryService _deliveryService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PlatformJwtSettings _jwtSettings;

    public PlatformPasswordResetService(
        IPlatformPasswordResetRepository repository,
        IPlatformUserRepository userRepository,
        IPlatformAuthRepository authRepository,
        IPlatformPermissionChecker permissionChecker,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IPasswordHashService passwordHashService,
        IPlatformPasswordPolicyValidator passwordPolicyValidator,
        IPlatformPasswordResetLinkBuilder linkBuilder,
        IPlatformPasswordResetDeliveryService deliveryService,
        IDateTimeProvider dateTimeProvider,
        PlatformJwtSettings jwtSettings)
    {
        _repository = repository;
        _userRepository = userRepository;
        _authRepository = authRepository;
        _permissionChecker = permissionChecker;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _passwordHashService = passwordHashService;
        _passwordPolicyValidator = passwordPolicyValidator;
        _linkBuilder = linkBuilder;
        _deliveryService = deliveryService;
        _dateTimeProvider = dateTimeProvider;
        _jwtSettings = jwtSettings;
    }

    public async Task<ApplicationResult<PlatformPasswordResetTokenIssueResult>> CreatePendingResetTokenAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await _repository.PlatformUserExistsAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformPasswordResetTokenIssueResult>.Failure(UserNotFound);
        }

        var now = _dateTimeProvider.UtcNow;
        var expiresAt = now.AddHours(PlatformPasswordResetConstants.DefaultLifetimeHours);
        var generatedToken = _refreshTokenGenerator.CreateRefreshToken(1);
        var tokenId = Guid.NewGuid();
        var tokenHash = _tokenHashService.HashToken(generatedToken.Token, _jwtSettings.SigningKey);

        var token = PlatformPasswordResetToken.CreatePending(
            tokenId,
            platformUserId,
            tokenHash,
            expiresAt,
            now);

        await _repository.AddPendingTokenAsync(token, cancellationToken);

        return ApplicationResult<PlatformPasswordResetTokenIssueResult>.Success(
            new PlatformPasswordResetTokenIssueResult(tokenId, generatedToken.Token, expiresAt));
    }

    public async Task<ApplicationResult<PlatformPasswordResetTokenValidationResult>> ValidateResetTokenAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        var token = await FindTokenByRawValueAsync(rawToken, cancellationToken);
        if (token is null)
        {
            return ApplicationResult<PlatformPasswordResetTokenValidationResult>.Failure(InvalidToken);
        }

        var validationError = ResolveValidationError(token, _dateTimeProvider.UtcNow);
        if (validationError is not null)
        {
            return ApplicationResult<PlatformPasswordResetTokenValidationResult>.Failure(validationError);
        }

        return ApplicationResult<PlatformPasswordResetTokenValidationResult>.Success(
            new PlatformPasswordResetTokenValidationResult(token.Id, token.PlatformUserId!.Value));
    }

    public async Task<ApplicationResult> MarkTokenUsedAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        var token = await FindTokenByRawValueAsync(rawToken, cancellationToken);
        if (token is null)
        {
            return ApplicationResult.Failure(InvalidToken);
        }

        var validationError = ResolveValidationError(token, _dateTimeProvider.UtcNow);
        if (validationError is not null)
        {
            return ApplicationResult.Failure(validationError);
        }

        var marked = await _repository.MarkUsedAsync(token.Id, _dateTimeProvider.UtcNow, cancellationToken);
        return marked
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(InvalidToken);
    }

    public async Task<ApplicationResult<int>> RevokeActivePendingTokensAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await _repository.PlatformUserExistsAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<int>.Failure(UserNotFound);
        }

        var revokedCount = await _repository.RevokeActivePendingTokensAsync(
            platformUserId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ApplicationResult<int>.Success(revokedCount);
    }

    public async Task<ApplicationResult<InitiatePlatformPasswordResetResponse>> InitiateAdminPasswordResetAsync(
        Guid targetUserId,
        Guid actorPlatformUserId,
        PlatformAuthClientContext? clientContext,
        CancellationToken cancellationToken)
    {
        if (!await _permissionChecker.HasPermissionAsync(
                actorPlatformUserId,
                PlatformPermissionCodes.UsersUpdate,
                cancellationToken))
        {
            await WriteAuditAsync(
                targetUserId,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                "ACCESS_DENIED",
                clientContext,
                cancellationToken);
            return ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(AccessDenied);
        }

        var user = await _userRepository.GetUserEntityByIdAsync(targetUserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(
                new ApplicationError("platform_users.not_found", "Platform user was not found."));
        }

        if (!IsEligibleForPasswordReset(user))
        {
            await WriteAuditAsync(
                targetUserId,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                "INVALID_USER_STATE",
                clientContext,
                cancellationToken);
            return ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(InvalidUserState);
        }

        await _repository.RevokeActivePendingTokensAsync(
            targetUserId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        var created = await CreatePendingResetTokenAsync(targetUserId, cancellationToken);
        if (created.IsFailure || created.Value is null)
        {
            await WriteAuditAsync(
                targetUserId,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                created.Error.Code,
                clientContext,
                cancellationToken);
            return ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(created.Error);
        }

        var resetUrl = _linkBuilder.BuildResetUrl(created.Value.RawToken);
        var delivery = await _deliveryService.DeliverAsync(
            new PlatformPasswordResetDeliveryRequest(
                targetUserId,
                user.Email,
                user.DisplayName,
                created.Value.RawToken,
                resetUrl,
                created.Value.ExpiresAt),
            cancellationToken);

        if (delivery.IsFailure || delivery.Value is null)
        {
            await WriteAuditAsync(
                targetUserId,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                delivery.Error.Code,
                clientContext,
                cancellationToken);
            return ApplicationResult<InitiatePlatformPasswordResetResponse>.Failure(delivery.Error);
        }

        await WriteAuditAsync(
            targetUserId,
            PlatformPasswordResetConstants.AuditMethod.PasswordResetRequested,
            PlatformPasswordResetConstants.AuditStatus.Success,
            null,
            clientContext,
            cancellationToken);

        return ApplicationResult<InitiatePlatformPasswordResetResponse>.Success(
            new InitiatePlatformPasswordResetResponse(
                targetUserId,
                user.Email,
                created.Value.ExpiresAt,
                delivery.Value.DeliveryMode,
                delivery.Value.ResetUrlForAdmin,
                delivery.Value.Message));
    }

    public async Task<ApplicationResult<ValidatePlatformPasswordResetTokenResponse>> ValidatePublicTokenAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        var token = await FindTokenByRawValueAsync(rawToken, cancellationToken);
        if (token is null)
        {
            return ApplicationResult<ValidatePlatformPasswordResetTokenResponse>.Success(
                new ValidatePlatformPasswordResetTokenResponse(
                    false,
                    PlatformPasswordResetConstants.TokenStatus.Invalid,
                    null));
        }

        var now = _dateTimeProvider.UtcNow;
        var validationError = ResolveValidationError(token, now);
        if (validationError is null)
        {
            return ApplicationResult<ValidatePlatformPasswordResetTokenResponse>.Success(
                new ValidatePlatformPasswordResetTokenResponse(
                    true,
                    PlatformPasswordResetConstants.TokenStatus.Pending,
                    token.ExpiresAt));
        }

        var status = validationError.Code switch
        {
            "platform_password_reset.token_used" => PlatformPasswordResetConstants.TokenStatus.Used,
            "platform_password_reset.token_revoked" => PlatformPasswordResetConstants.TokenStatus.Revoked,
            "platform_password_reset.token_expired" => PlatformPasswordResetConstants.TokenStatus.Expired,
            _ => PlatformPasswordResetConstants.TokenStatus.Invalid
        };

        return ApplicationResult<ValidatePlatformPasswordResetTokenResponse>.Success(
            new ValidatePlatformPasswordResetTokenResponse(false, status, token.ExpiresAt));
    }

    public async Task<ApplicationResult<CompletePlatformPasswordResetResponse>> CompletePasswordResetAsync(
        CompletePlatformPasswordResetRequest request,
        PlatformAuthClientContext? clientContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            await WriteAuditAsync(
                null,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                "PASSWORD_MISMATCH",
                clientContext,
                cancellationToken);
            return ApplicationResult<CompletePlatformPasswordResetResponse>.Failure(PasswordMismatch);
        }

        var policyError = _passwordPolicyValidator.Validate(request.NewPassword);
        if (policyError is not null)
        {
            await WriteAuditAsync(
                null,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                "PASSWORD_POLICY",
                clientContext,
                cancellationToken);
            return ApplicationResult<CompletePlatformPasswordResetResponse>.Failure(policyError);
        }

        var validation = await ValidateResetTokenAsync(request.Token, cancellationToken);
        if (validation.IsFailure || validation.Value is null)
        {
            await WriteAuditAsync(
                null,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                validation.Error.Code,
                clientContext,
                cancellationToken);
            return ApplicationResult<CompletePlatformPasswordResetResponse>.Failure(validation.Error);
        }

        var user = await _userRepository.GetUserEntityByIdAsync(validation.Value.PlatformUserId, cancellationToken);
        if (user is null || !IsEligibleForPasswordReset(user))
        {
            await WriteAuditAsync(
                validation.Value.PlatformUserId,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                "INVALID_USER_STATE",
                clientContext,
                cancellationToken);
            return ApplicationResult<CompletePlatformPasswordResetResponse>.Failure(InvalidUserState);
        }

        var now = _dateTimeProvider.UtcNow;
        var passwordHash = _passwordHashService.HashPassword(request.NewPassword);
        user.SetPasswordHash(passwordHash, now);
        if (user.Status == PlatformAuthConstants.LockedStatus)
        {
            user.SetStatus(PlatformAuthConstants.ActiveStatus, now);
        }

        await _userRepository.UpdateUserAsync(user, cancellationToken);

        var marked = await MarkTokenUsedAsync(request.Token, cancellationToken);
        if (marked.IsFailure)
        {
            await WriteAuditAsync(
                user.Id,
                PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed,
                PlatformPasswordResetConstants.AuditStatus.Failed,
                marked.Error.Code,
                clientContext,
                cancellationToken);
            return ApplicationResult<CompletePlatformPasswordResetResponse>.Failure(marked.Error);
        }

        await _repository.RevokeActivePendingTokensAsync(user.Id, now, cancellationToken);

        var revokedSessions = await _authRepository.RevokeAllSessionsForUserAsync(
            user.Id,
            now,
            cancellationToken,
            revokedByPlatformUserId: user.Id,
            revokeReason: PlatformAuthAlignmentConstants.RevokeReason.PasswordReset);

        await WriteAuditAsync(
            user.Id,
            PlatformPasswordResetConstants.AuditMethod.SessionsRevoked,
            PlatformPasswordResetConstants.AuditStatus.Success,
            $"sessions_revoked={revokedSessions}",
            clientContext,
            cancellationToken);

        await WriteAuditAsync(
            user.Id,
            PlatformPasswordResetConstants.AuditMethod.PasswordResetCompleted,
            PlatformPasswordResetConstants.AuditStatus.Success,
            null,
            clientContext,
            cancellationToken);

        return ApplicationResult<CompletePlatformPasswordResetResponse>.Success(
            new CompletePlatformPasswordResetResponse(
                true,
                "Password has been reset successfully. Sign in with your new password."));
    }

    private static bool IsEligibleForPasswordReset(PlatformUser user)
    {
        if (user.Status is PlatformAuthConstants.DeletedStatus or PlatformAuthConstants.InactiveStatus)
        {
            return false;
        }

        if (string.Equals(
                user.PasswordHash,
                PlatformUserConstants.PendingInvitePasswordHash,
                StringComparison.Ordinal))
        {
            return false;
        }

        return user.Status is PlatformAuthConstants.ActiveStatus or PlatformAuthConstants.LockedStatus;
    }

    private async Task WriteAuditAsync(
        Guid? platformUserId,
        string authenticationMethod,
        string loginStatus,
        string? failureReason,
        PlatformAuthClientContext? clientContext,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var audit = PlatformLoginAudit.Create(
            Guid.NewGuid(),
            platformUserId,
            loginStatus,
            now,
            authenticationMethod: authenticationMethod,
            attemptedAt: now,
            ipAddress: clientContext?.IpAddress,
            userAgent: clientContext?.UserAgent,
            failureReason: failureReason);

        await _authRepository.SaveFailedLoginAuditAsync(audit, cancellationToken);
    }

    private async Task<PlatformPasswordResetToken?> FindTokenByRawValueAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = _tokenHashService.HashToken(rawToken.Trim(), _jwtSettings.SigningKey);
        return await _repository.FindByTokenHashAsync(tokenHash, cancellationToken);
    }

    private static ApplicationError? ResolveValidationError(PlatformPasswordResetToken token, DateTimeOffset now)
    {
        if (token.Status == PlatformAuthConstants.UsedTokenStatus || token.UsedAt is not null)
        {
            return TokenUsed;
        }

        if (token.Status == PlatformAuthConstants.RevokedTokenStatus || token.RevokedAt is not null)
        {
            return TokenRevoked;
        }

        if (token.Status == PlatformAuthConstants.ExpiredTokenStatus ||
            token.ExpiresAt is null ||
            token.ExpiresAt <= now)
        {
            return TokenExpired;
        }

        if (token.Status != PlatformAuthConstants.PendingTokenStatus)
        {
            return InvalidToken;
        }

        return null;
    }
}
