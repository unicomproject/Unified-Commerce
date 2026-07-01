using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Infrastructure.Modules.PlatformAdministration.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly PlatformJwtOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenService(IOptions<PlatformJwtOptions> options, IDateTimeProvider dateTimeProvider)
    {
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var token = Base64Url.Encode(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenResult(token, _dateTimeProvider.UtcNow.AddDays(_options.RefreshTokenDays));
    }
}
