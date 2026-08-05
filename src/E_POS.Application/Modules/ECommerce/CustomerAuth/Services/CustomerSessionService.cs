using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerSessionService : ICustomerSessionService
{
    private readonly ICustomerSessionRepository _repository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICustomerTokenFactory _tokenFactory;
    private readonly CustomerJwtSettings _jwtSettings;

    public CustomerSessionService(
        ICustomerSessionRepository repository,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        ICustomerTokenFactory tokenFactory,
        CustomerJwtSettings jwtSettings)
    {
        _repository = repository;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
        _tokenFactory = tokenFactory;
        _jwtSettings = jwtSettings;
    }

    public async Task<ApplicationResult<CustomerAuthTokenResult>> RefreshAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(refreshToken))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidRefreshToken);

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
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidRefreshToken);
        }

        var accessToken = _tokenFactory.CreateAccessToken(rotation.Account, rotation.SessionId.Value);
        return ApplicationResult<CustomerAuthTokenResult>.Success(
            _tokenFactory.CreateTokenResult(
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
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidSession);

        var revoked = await _repository.RevokeSessionAsync(
            tenantId, customerId, sessionId, _dateTimeProvider.UtcNow, cancellationToken);
        return revoked ? ApplicationResult.Success() : ApplicationResult.Failure(CustomerAuthErrors.InvalidSession);
    }
}
