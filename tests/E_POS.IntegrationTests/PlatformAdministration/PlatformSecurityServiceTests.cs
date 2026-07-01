using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Common.Security;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformSecurityServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 4, 30, 0, TimeSpan.Zero);

    [Fact]
    public void PasswordHashService_VerifiesCorrectPasswordAndRejectsWrongPassword()
    {
        var service = new PasswordHashService();

        var hash = service.HashPassword("correct-password");

        Assert.NotEqual("correct-password", hash);
        Assert.True(service.VerifyPassword("correct-password", hash));
        Assert.False(service.VerifyPassword("wrong-password", hash));
    }

    [Fact]
    public void JwtTokenFactory_CreatesJwtWithPlatformClaimsAndPermissions()
    {
        var service = new JwtTokenFactory(new FakeDateTimeProvider());
        var user = PlatformUser.Create(Guid.NewGuid(), "admin@tmepos.test", "hash", PlatformAuthConstants.ActiveStatus, Now);
        var sessionId = Guid.NewGuid();
        const string jwtId = "jwt-id-123";

        var token = service.CreateAccessToken(new JwtTokenDescriptor(
            "TM-EPOS",
            "TM-EPOS-Platform",
            "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
            15,
            new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["identity_type"] = PlatformAuthConstants.IdentityType,
                ["session_id"] = sessionId.ToString(),
                ["jti"] = jwtId,
                ["permissions"] = new[] { "platform.users.manage" }
            }));

        var parts = token.AccessToken.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.Equal(Now.AddMinutes(15), token.ExpiresAt);

        using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));
        var root = payload.RootElement;
        Assert.Equal("TM-EPOS", root.GetProperty("iss").GetString());
        Assert.Equal("TM-EPOS-Platform", root.GetProperty("aud").GetString());
        Assert.Equal(user.Id.ToString(), root.GetProperty("sub").GetString());
        Assert.Equal(user.Email, root.GetProperty("email").GetString());
        Assert.Equal(PlatformAuthConstants.IdentityType, root.GetProperty("identity_type").GetString());
        Assert.Equal(sessionId.ToString(), root.GetProperty("session_id").GetString());
        Assert.Equal(jwtId, root.GetProperty("jti").GetString());
        Assert.Equal("platform.users.manage", root.GetProperty("permissions")[0].GetString());
    }

    private static string DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}