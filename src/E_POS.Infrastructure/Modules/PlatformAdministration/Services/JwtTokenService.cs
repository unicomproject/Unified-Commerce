using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Modules.PlatformAdministration.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly PlatformJwtOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenService(IOptions<PlatformJwtOptions> options, IDateTimeProvider dateTimeProvider)
    {
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    public JwtTokenResult CreateAccessToken(
        PlatformUser user,
        Guid sessionId,
        string jwtId,
        IReadOnlyList<string> permissions)
    {
        EnsureOptionsAreValid();

        var now = _dateTimeProvider.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        // Platform JWTs carry identity, session, token id, and permission claims.
        var payload = new Dictionary<string, object>
        {
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["identity_type"] = PlatformAuthConstants.IdentityType,
            ["session_id"] = sessionId.ToString(),
            ["jti"] = jwtId,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds(),
            ["permissions"] = permissions
        };

        var headerSegment = Base64Url.Encode(JsonSerializer.Serialize(header));
        var payloadSegment = Base64Url.Encode(JsonSerializer.Serialize(payload));
        var unsignedToken = $"{headerSegment}.{payloadSegment}";
        var signature = Sign(unsignedToken);

        return new JwtTokenResult($"{unsignedToken}.{signature}", expiresAt);
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return Base64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private void EnsureOptionsAreValid()
    {
        if (string.IsNullOrWhiteSpace(_options.Issuer) ||
            string.IsNullOrWhiteSpace(_options.Audience) ||
            string.IsNullOrWhiteSpace(_options.SigningKey) ||
            Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException("Platform JWT settings are not configured correctly.");
        }
    }
}

