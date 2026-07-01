using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;

namespace E_POS.Infrastructure.Common.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenGenerator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public RefreshTokenResult CreateRefreshToken(int lifetimeDays)
    {
        // Generate a high-entropy refresh token for server-side session renewal.
        var token = Base64Url.Encode(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenResult(token, _dateTimeProvider.UtcNow.AddDays(lifetimeDays));
    }
}