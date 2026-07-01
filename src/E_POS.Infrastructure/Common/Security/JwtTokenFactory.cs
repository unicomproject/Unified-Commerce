using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;

namespace E_POS.Infrastructure.Common.Security;

public sealed class JwtTokenFactory : IJwtTokenFactory
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenFactory(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public JwtTokenResult CreateAccessToken(JwtTokenDescriptor descriptor)
    {
        EnsureDescriptorIsValid(descriptor);

        var now = _dateTimeProvider.UtcNow;
        var expiresAt = now.AddMinutes(descriptor.AccessTokenMinutes);
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object>(descriptor.Claims)
        {
            ["iss"] = descriptor.Issuer,
            ["aud"] = descriptor.Audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds()
        };

        var headerSegment = Base64Url.Encode(JsonSerializer.Serialize(header));
        var payloadSegment = Base64Url.Encode(JsonSerializer.Serialize(payload));
        var unsignedToken = $"{headerSegment}.{payloadSegment}";
        var signature = Sign(unsignedToken, descriptor.SigningKey);

        return new JwtTokenResult($"{unsignedToken}.{signature}", expiresAt);
    }

    private static string Sign(string value, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return Base64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static void EnsureDescriptorIsValid(JwtTokenDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Issuer) ||
            string.IsNullOrWhiteSpace(descriptor.Audience) ||
            string.IsNullOrWhiteSpace(descriptor.SigningKey) ||
            Encoding.UTF8.GetByteCount(descriptor.SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT settings are not configured correctly.");
        }
    }
}