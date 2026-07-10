using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformPasswordResetService : IPlatformPasswordResetService
{
    private static readonly ApplicationError UserNotFound = new(
        "platform_password_reset.user_not_found",
        "Platform user was not found.");

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

    private readonly IPlatformPasswordResetRepository _repository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PlatformJwtSettings _jwtSettings;

    public PlatformPasswordResetService(
        IPlatformPasswordResetRepository repository,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        PlatformJwtSettings jwtSettings)
    {
        _repository = repository;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
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
