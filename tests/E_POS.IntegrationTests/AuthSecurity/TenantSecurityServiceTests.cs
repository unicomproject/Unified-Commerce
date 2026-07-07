using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public void TenantAuthSession_Revoke_SetsRevokedStatusAndUpdatedAt()
    {
        var session = TenantAuthSession.Create(Guid.NewGuid(), Guid.NewGuid(), "session-hash", Now);
        var revokedAt = Now.AddMinutes(5);

        session.Revoke(revokedAt);

        Assert.Equal(TenantAuthConstants.RevokedTokenStatus, session.Status);
        Assert.Equal(revokedAt, session.UpdatedAt);
    }

    [Fact]
    public void TenantRefreshToken_Revoke_SetsRevokedStatusAndUpdatedAt()
    {
        var refreshToken = TenantRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "refresh-hash", Now.AddDays(7), Now);
        var revokedAt = Now.AddMinutes(5);

        refreshToken.Revoke(revokedAt);

        Assert.Equal(TenantAuthConstants.RevokedTokenStatus, refreshToken.Status);
        Assert.Equal(revokedAt, refreshToken.UpdatedAt);
        Assert.Equal(Now.AddDays(7), refreshToken.ExpiresAt);
    }

    [Fact]
    public async Task AuthSessionValidator_WithActiveTenantUserAndSession_ReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.Tenants.Add(CreateTenant(tenantId, TenantAuthConstants.ActiveTenantStatus));
        dbContext.TenantUsers.Add(TenantUser.Create(tenantUserId, tenantId, "user@tenant.test", null, "hash", TenantAuthConstants.ActiveUserStatus, Now));
        dbContext.TenantAuthSessions.Add(TenantAuthSession.Create(sessionId, tenantUserId, "session-hash", Now));
        await dbContext.SaveChangesAsync();

        var validator = new AuthSessionValidator(dbContext);

        var isActive = await validator.IsCurrentSessionActiveAsync(CreateTenantPrincipal(tenantUserId, tenantId, sessionId), CancellationToken.None);

        Assert.True(isActive);
    }

    [Fact]
    public async Task AuthSessionValidator_WithLockedTenantUser_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.Tenants.Add(CreateTenant(tenantId, TenantAuthConstants.ActiveTenantStatus));
        dbContext.TenantUsers.Add(TenantUser.Create(tenantUserId, tenantId, "locked@tenant.test", null, "hash", TenantAuthConstants.LockedUserStatus, Now));
        dbContext.TenantAuthSessions.Add(TenantAuthSession.Create(sessionId, tenantUserId, "session-hash", Now));
        await dbContext.SaveChangesAsync();

        var validator = new AuthSessionValidator(dbContext);

        var isActive = await validator.IsCurrentSessionActiveAsync(CreateTenantPrincipal(tenantUserId, tenantId, sessionId), CancellationToken.None);

        Assert.False(isActive);
    }

    [Fact]
    public async Task AuthSessionValidator_WithInactiveTenant_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.Tenants.Add(CreateTenant(tenantId, "suspended"));
        dbContext.TenantUsers.Add(TenantUser.Create(tenantUserId, tenantId, "user@tenant.test", null, "hash", TenantAuthConstants.ActiveUserStatus, Now));
        dbContext.TenantAuthSessions.Add(TenantAuthSession.Create(sessionId, tenantUserId, "session-hash", Now));
        await dbContext.SaveChangesAsync();

        var validator = new AuthSessionValidator(dbContext);

        var isActive = await validator.IsCurrentSessionActiveAsync(CreateTenantPrincipal(tenantUserId, tenantId, sessionId), CancellationToken.None);

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

    private static ClaimsPrincipal CreateTenantPrincipal(Guid tenantUserId, Guid tenantId, Guid sessionId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", tenantUserId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("identity_type", TenantAuthConstants.IdentityType),
                new Claim("session_id", sessionId.ToString())
            ],
            "Test"));
    }

    private static Tenant CreateTenant(Guid tenantId, string status)
    {
        var tenant = new Tenant();
        SetProperty(tenant, nameof(BaseEntity.Id), tenantId);
        SetProperty(tenant, nameof(Tenant.TenantCode), $"TENANT{tenantId:N}"[..20]);
        SetProperty(tenant, nameof(Tenant.CurrencyCode), "LKR");
        SetProperty(tenant, nameof(Tenant.Name), "Test Tenant");
        SetProperty(tenant, nameof(Tenant.Status), status);
        SetProperty(tenant, nameof(Tenant.BaseCurrency), "LKR");
        SetProperty(tenant, nameof(Tenant.BillingStatus), "ACTIVE");
        SetProperty(tenant, nameof(Tenant.BusinessTypeId), Guid.NewGuid());
        SetProperty(tenant, nameof(Tenant.DefaultLocale), "en-LK");
        SetProperty(tenant, nameof(Tenant.DefaultTimezone), "Asia/Colombo");
        SetProperty(tenant, nameof(Tenant.OperatingMode), "POS");
        SetProperty(tenant, nameof(Tenant.PrimaryDomain), "tenant.test");
        SetProperty(tenant, nameof(AuditableEntity.CreatedAt), Now);
        SetProperty(tenant, nameof(AuditableEntity.UpdatedAt), Now);
        return tenant;
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        var type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
        while (type is not null)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null)
            {
                property.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }

        throw new InvalidOperationException($"Property '{propertyName}' was not found.");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}


