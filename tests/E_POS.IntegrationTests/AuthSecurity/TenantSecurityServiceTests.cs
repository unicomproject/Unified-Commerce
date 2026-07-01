using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Domain.Modules.AuthSecurity.Constants;
using E_POS.Infrastructure.Common.Security;
using Xunit;

namespace E_POS.IntegrationTests.AuthSecurity;

public sealed class TenantSecurityServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void JwtTokenFactory_CreatesJwtWithTenantClaimsAndPermissions()
    {
        var service = new JwtTokenFactory(new FakeDateTimeProvider());
        var account = new TenantLoginAccount(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USER@TENANT.TEST",
            "hash",
            TenantAuthConstants.ActiveUserStatus,
            TenantAuthConstants.ActiveTenantStatus);
        var sessionId = Guid.NewGuid();
        const string jwtId = "tenant-jwt-id-123";

        var token = service.CreateAccessToken(new JwtTokenDescriptor(
            "TM-EPOS",
            "TM-EPOS-Tenant",
            "TEST_TENANT_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
            15,
            new Dictionary<string, object>
            {
                ["sub"] = account.TenantUserId.ToString(),
                ["tenant_id"] = account.TenantId.ToString(),
                ["email"] = account.Email,
                ["identity_type"] = TenantAuthConstants.IdentityType,
                ["session_id"] = sessionId.ToString(),
                ["jti"] = jwtId,
                ["permissions"] = new[] { "tenant.dashboard.view" }
            }));

        var parts = token.AccessToken.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.Equal(Now.AddMinutes(15), token.ExpiresAt);

        using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));
        var root = payload.RootElement;
        Assert.Equal("TM-EPOS", root.GetProperty("iss").GetString());
        Assert.Equal("TM-EPOS-Tenant", root.GetProperty("aud").GetString());
        Assert.Equal(account.TenantUserId.ToString(), root.GetProperty("sub").GetString());
        Assert.Equal(account.TenantId.ToString(), root.GetProperty("tenant_id").GetString());
        Assert.Equal(account.Email, root.GetProperty("email").GetString());
        Assert.Equal(TenantAuthConstants.IdentityType, root.GetProperty("identity_type").GetString());
        Assert.Equal(sessionId.ToString(), root.GetProperty("session_id").GetString());
        Assert.Equal(jwtId, root.GetProperty("jti").GetString());
        Assert.Equal("tenant.dashboard.view", root.GetProperty("permissions")[0].GetString());
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