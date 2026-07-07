using System.Text;
using System.Text.Json;
using System.Security.Claims;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public void PlatformAuthSession_Revoke_SetsRevokedStatusAndUpdatedAt()
    {
        var session = PlatformAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "session-hash", Now);
        var revokedAt = Now.AddMinutes(5);

        session.Revoke(revokedAt);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
        Assert.Equal(revokedAt, session.UpdatedAt);
    }

    [Fact]
    public void PlatformRefreshToken_Revoke_SetsRevokedStatusAndUpdatedAt()
    {
        var refreshToken = PlatformRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "refresh-hash", Now.AddDays(7), Now);
        var revokedAt = Now.AddMinutes(5);

        refreshToken.Revoke(revokedAt);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, refreshToken.Status);
        Assert.Equal(revokedAt, refreshToken.UpdatedAt);
        Assert.Equal(Now.AddDays(7), refreshToken.ExpiresAt);
    }


    [Fact]
    public async Task AuthSessionValidator_WithActivePlatformUserAndSession_ReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.PlatformUsers.Add(PlatformUser.Create(userId, "admin@tmepos.test", "hash", PlatformAuthConstants.ActiveStatus, Now));
        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(sessionId, userId, "session-hash", Now));
        await dbContext.SaveChangesAsync();

        var validator = new AuthSessionValidator(dbContext);

        var isActive = await validator.IsCurrentSessionActiveAsync(CreatePlatformPrincipal(userId, sessionId), CancellationToken.None);

        Assert.True(isActive);
    }

    [Fact]
    public async Task AuthSessionValidator_WithLockedPlatformUser_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.PlatformUsers.Add(PlatformUser.Create(userId, "locked@tmepos.test", "hash", PlatformAuthConstants.LockedStatus, Now));
        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(sessionId, userId, "session-hash", Now));
        await dbContext.SaveChangesAsync();

        var validator = new AuthSessionValidator(dbContext);

        var isActive = await validator.IsCurrentSessionActiveAsync(CreatePlatformPrincipal(userId, sessionId), CancellationToken.None);

        Assert.False(isActive);
    }
    private static string DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }


    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private static ClaimsPrincipal CreatePlatformPrincipal(Guid userId, Guid sessionId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("identity_type", PlatformAuthConstants.IdentityType),
            new Claim("session_id", sessionId.ToString())
        }, "Test"));
    }
    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
