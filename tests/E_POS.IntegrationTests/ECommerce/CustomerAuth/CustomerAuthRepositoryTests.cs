using System.Security.Claims;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.IntegrationTests.ECommerce.CustomerAuth;

public sealed class CustomerAuthRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FindLoginAccountAsync_UsesNormalizedIdentifierAndTenantBoundary()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedAccountAsync(dbContext, tenantId);
        var repository = new CustomerLoginRepository(dbContext);

        var byEmail = await repository.FindLoginAccountAsync(
            tenantId, "CUSTOMER@EXAMPLE.COM", string.Empty, CancellationToken.None);
        var byPhone = await repository.FindLoginAccountAsync(
            tenantId, string.Empty, "+94771234567", CancellationToken.None);
        var otherTenant = await repository.FindLoginAccountAsync(
            Guid.NewGuid(), "CUSTOMER@EXAMPLE.COM", string.Empty, CancellationToken.None);

        Assert.NotNull(byEmail);
        Assert.NotNull(byPhone);
        Assert.Equal(tenantId, byEmail!.TenantId);
        Assert.Equal(byEmail.CustomerId, byPhone!.CustomerId);
        Assert.Null(otherTenant);
    }

    [Fact]
    public async Task SaveSuccessfulLoginAsync_PersistsSessionAndAccountLoginMetadata()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedAccountAsync(dbContext, tenantId);
        dbContext.ChangeTracker.Clear();
        var repository = new CustomerLoginRepository(dbContext);
        var loginAccount = await repository.FindLoginAccountAsync(
            tenantId, "CUSTOMER@EXAMPLE.COM", string.Empty, CancellationToken.None);
        Assert.NotNull(loginAccount);
        loginAccount!.Account.RecordSuccessfulLogin(Now);
        var session = CustomerAuthSession.Create(
            Guid.NewGuid(),
            tenantId,
            loginAccount.Account.Id,
            "session-hash",
            null,
            "integration-test",
            "Browser",
            Now.AddDays(30),
            Now);
        var refreshToken = CustomerRefreshToken.Create(
            Guid.NewGuid(),
            tenantId,
            session.Id,
            "refresh-token-hash",
            Guid.NewGuid(),
            Now.AddDays(30),
            Now);

        await repository.SaveSuccessfulLoginAsync(
            loginAccount.Account,
            session,
            refreshToken,
            CancellationToken.None);

        dbContext.ChangeTracker.Clear();
        var storedSession = await dbContext.CustomerAuthSessions.SingleAsync();
        var storedAccount = await dbContext.CustomerAuthAccounts.SingleAsync();
        var storedRefreshToken = await dbContext.CustomerRefreshTokens.SingleAsync();
        Assert.Equal("ACTIVE", storedSession.Status);
        Assert.Equal(Now.AddDays(30), storedSession.ExpiresAt);
        Assert.Equal(Now, storedAccount.LastLoginAt);
        Assert.Equal("refresh-token-hash", storedRefreshToken.TokenHash);
        Assert.Equal("ACTIVE", storedRefreshToken.Status);
    }

    [Fact]
    public async Task RevokedSession_IsRejectedByCustomerSessionValidator()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var seeded = await SeedAccountAsync(dbContext, tenantId);
        var repository = new CustomerSessionRepository(dbContext);
        var sessionId = Guid.NewGuid();
        var session = CustomerAuthSession.Create(
            sessionId,
            tenantId,
            seeded.Account.Id,
            "validator-session-hash",
            null,
            null,
            null,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow);
        dbContext.CustomerAuthSessions.Add(session);
        dbContext.CustomerRefreshTokens.Add(CustomerRefreshToken.Create(
            Guid.NewGuid(),
            tenantId,
            sessionId,
            "logout-refresh-token-hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", seeded.CustomerId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("session_id", sessionId.ToString()),
            new Claim("identity_type", "customer")
        ], "Test"));
        var validator = new AuthSessionValidator(dbContext);

        Assert.True(await validator.IsCurrentSessionActiveAsync(
            principal, CancellationToken.None));

        var revoked = await repository.RevokeSessionAsync(
            tenantId,
            seeded.CustomerId,
            sessionId,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.True(revoked);
        dbContext.ChangeTracker.Clear();
        var revokedSession = await dbContext.CustomerAuthSessions
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal("REVOKED", revokedSession.Status);
        Assert.NotNull(revokedSession.RevokedAt);
        var revokedRefreshToken = await dbContext.CustomerRefreshTokens
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal("REVOKED", revokedRefreshToken.Status);
        Assert.NotNull(revokedRefreshToken.RevokedAt);
        Assert.False(await validator.IsCurrentSessionActiveAsync(
            principal, CancellationToken.None));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ConsumesCurrentTokenAndExtendsSession()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var seeded = await SeedRefreshSessionAsync(
            dbContext,
            tenantId,
            "current-token-hash");
        dbContext.ChangeTracker.Clear();
        var repository = new CustomerSessionRepository(dbContext);
        var replacementId = Guid.NewGuid();
        var replacementExpiresAt = Now.AddDays(30).AddMinutes(1);

        var result = await repository.RotateRefreshTokenAsync(
            tenantId,
            "current-token-hash",
            replacementId,
            "replacement-token-hash",
            replacementExpiresAt,
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.Equal(CustomerRefreshRotationStatus.Succeeded, result.Status);
        Assert.Equal(seeded.SessionId, result.SessionId);
        Assert.Equal(seeded.CustomerId, result.Account!.CustomerId);
        dbContext.ChangeTracker.Clear();
        var current = await dbContext.CustomerRefreshTokens
            .SingleAsync(x => x.Id == seeded.RefreshTokenId);
        var replacement = await dbContext.CustomerRefreshTokens
            .SingleAsync(x => x.Id == replacementId);
        var session = await dbContext.CustomerAuthSessions
            .SingleAsync(x => x.Id == seeded.SessionId);
        Assert.Equal("USED", current.Status);
        Assert.Equal(replacementId, current.ReplacedByTokenId);
        Assert.Equal(Now.AddMinutes(1), current.UsedAt);
        Assert.Equal("ACTIVE", replacement.Status);
        Assert.Equal(seeded.TokenFamilyId, replacement.TokenFamilyId);
        Assert.Equal(replacementExpiresAt, session.ExpiresAt);
        Assert.Equal(Now.AddMinutes(1), session.LastActivityAt);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ReusedToken_RevokesSessionAndTokenFamily()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var seeded = await SeedRefreshSessionAsync(
            dbContext,
            tenantId,
            "reused-token-hash");
        dbContext.ChangeTracker.Clear();
        var repository = new CustomerSessionRepository(dbContext);

        var first = await repository.RotateRefreshTokenAsync(
            tenantId,
            "reused-token-hash",
            Guid.NewGuid(),
            "first-replacement-hash",
            Now.AddDays(30).AddMinutes(1),
            Now.AddMinutes(1),
            CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        var reuse = await repository.RotateRefreshTokenAsync(
            tenantId,
            "reused-token-hash",
            Guid.NewGuid(),
            "unused-replacement-hash",
            Now.AddDays(30).AddMinutes(2),
            Now.AddMinutes(2),
            CancellationToken.None);

        Assert.Equal(CustomerRefreshRotationStatus.Succeeded, first.Status);
        Assert.Equal(CustomerRefreshRotationStatus.Reused, reuse.Status);
        dbContext.ChangeTracker.Clear();
        var session = await dbContext.CustomerAuthSessions.SingleAsync();
        var family = await dbContext.CustomerRefreshTokens
            .Where(x => x.TokenFamilyId == seeded.TokenFamilyId)
            .ToListAsync();
        Assert.Equal("REVOKED", session.Status);
        Assert.Equal("REFRESH_TOKEN_REUSE", session.RevokedReason);
        Assert.All(family, token =>
        {
            Assert.Equal("REVOKED", token.Status);
            Assert.NotNull(token.RevokedAt);
        });
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_OtherTenantCannotUseToken()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var seeded = await SeedRefreshSessionAsync(
            dbContext,
            tenantId,
            "tenant-scoped-token-hash");
        await SeedAccountAsync(dbContext, otherTenantId);
        dbContext.ChangeTracker.Clear();
        var repository = new CustomerSessionRepository(dbContext);

        var result = await repository.RotateRefreshTokenAsync(
            otherTenantId,
            "tenant-scoped-token-hash",
            Guid.NewGuid(),
            "replacement-hash",
            Now.AddDays(30),
            Now,
            CancellationToken.None);

        Assert.Equal(CustomerRefreshRotationStatus.Invalid, result.Status);
        dbContext.ChangeTracker.Clear();
        var original = await dbContext.CustomerRefreshTokens
            .SingleAsync(x => x.Id == seeded.RefreshTokenId);
        Assert.Equal("ACTIVE", original.Status);
        Assert.Null(original.UsedAt);
    }


    [Fact]
    public async Task RegisterExternalCustomerAsync_PersistsExternalAuthLinkSessionAndRefreshToken()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var authAccountId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(TenantEntity.Create(
            tenantId,
            $"T{tenantId:N}"[..12],
            $"tenant-{tenantId:N}",
            "Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
        var customer = CustomerEntity.CreateECommerceCustomer(
            customerId,
            tenantId,
            "CUS100001",
            "google@example.com",
            "Google",
            "Customer",
            Now);
        var account = CustomerAuthAccount.CreateExternal(authAccountId, tenantId, customerId, Now);
        account.RecordSuccessfulLogin(Now);
        var external = CustomerExternalAuthAccount.Create(
            Guid.NewGuid(),
            tenantId,
            authAccountId,
            CustomerExternalAuthAccount.GoogleProviderCode,
            "google-sub-001",
            "google@example.com",
            true,
            Now);
        external.RecordSuccessfulLogin("google@example.com", true, Now);
        var session = CustomerAuthSession.Create(
            sessionId,
            tenantId,
            authAccountId,
            "external-session-hash",
            null,
            "browser-agent",
            "Chrome",
            Now.AddDays(30),
            Now);
        var refreshToken = CustomerRefreshToken.Create(
            Guid.NewGuid(),
            tenantId,
            sessionId,
            "external-refresh-hash",
            Guid.NewGuid(),
            Now.AddDays(30),
            Now);
        var consents = new[]
        {
            CustomerConsent.Grant(Guid.NewGuid(), tenantId, customerId, "TERMS", null, null, "ECOMMERCE", null, null, Now),
            CustomerConsent.Grant(Guid.NewGuid(), tenantId, customerId, "PRIVACY", null, null, "ECOMMERCE", null, null, Now)
        };
        var repository = new CustomerExternalAuthRepository(dbContext);

        var saved = await repository.RegisterExternalCustomerAsync(
            customer,
            account,
            external,
            consents,
            session,
            refreshToken,
            CancellationToken.None);

        Assert.True(saved);
        dbContext.ChangeTracker.Clear();
        var storedExternal = await dbContext.CustomerExternalAuthAccounts.SingleAsync();
        var storedAccount = await dbContext.CustomerAuthAccounts.SingleAsync();
        Assert.Equal("GOOGLE", storedExternal.ProviderCode);
        Assert.Equal("google-sub-001", storedExternal.ProviderSubject);
        Assert.Equal(Now, storedExternal.LastLoginAt);
        Assert.Equal(Now, storedAccount.EmailVerifiedAt);
        Assert.Equal(Now, storedAccount.LastLoginAt);
        Assert.Equal("external-session-hash", await dbContext.CustomerAuthSessions.Select(x => x.SessionTokenHash).SingleAsync());
        Assert.Equal("external-refresh-hash", await dbContext.CustomerRefreshTokens.Select(x => x.TokenHash).SingleAsync());
        Assert.Equal(2, await dbContext.CustomerConsents.CountAsync());
    }

    [Fact]
    public async Task FindExternalLoginAccountAsync_UsesProviderSubjectAndTenantBoundary()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var seeded = await SeedAccountAsync(dbContext, tenantId);
        await SeedAccountAsync(dbContext, otherTenantId);
        dbContext.CustomerExternalAuthAccounts.Add(CustomerExternalAuthAccount.Create(
            Guid.NewGuid(),
            tenantId,
            seeded.Account.Id,
            CustomerExternalAuthAccount.GoogleProviderCode,
            "google-sub-tenant",
            "customer@example.com",
            true,
            Now));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new CustomerExternalAuthRepository(dbContext);

        var found = await repository.FindExternalLoginAccountAsync(
            tenantId,
            CustomerExternalAuthAccount.GoogleProviderCode,
            "google-sub-tenant",
            trackAccount: false,
            trackExternalAccount: false,
            CancellationToken.None);
        var otherTenant = await repository.FindExternalLoginAccountAsync(
            otherTenantId,
            CustomerExternalAuthAccount.GoogleProviderCode,
            "google-sub-tenant",
            trackAccount: false,
            trackExternalAccount: false,
            CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(seeded.CustomerId, found!.Account.CustomerId);
        Assert.Equal("google-sub-tenant", found.ExternalAccount.ProviderSubject);
        Assert.Null(otherTenant);
    }
    private static async Task<SeededRefreshSession> SeedRefreshSessionAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        string tokenHash)
    {
        var seededAccount = await SeedAccountAsync(dbContext, tenantId);
        var sessionId = Guid.NewGuid();
        var tokenFamilyId = Guid.NewGuid();
        var refreshTokenId = Guid.NewGuid();
        dbContext.CustomerAuthSessions.Add(CustomerAuthSession.Create(
            sessionId,
            tenantId,
            seededAccount.Account.Id,
            $"session-{sessionId:N}",
            null,
            null,
            null,
            Now.AddDays(30),
            Now));
        dbContext.CustomerRefreshTokens.Add(CustomerRefreshToken.Create(
            refreshTokenId,
            tenantId,
            sessionId,
            tokenHash,
            tokenFamilyId,
            Now.AddDays(30),
            Now));
        await dbContext.SaveChangesAsync();
        return new SeededRefreshSession(
            seededAccount.CustomerId,
            sessionId,
            refreshTokenId,
            tokenFamilyId);
    }

    private static async Task<SeededAccount> SeedAccountAsync(
        EPosDbContext dbContext,
        Guid tenantId)
    {
        var customerId = Guid.NewGuid();
        var account = CustomerAuthAccount.Create(
            Guid.NewGuid(), tenantId, customerId, "password-hash", Now);
        dbContext.Tenants.Add(TenantEntity.Create(
            tenantId,
            $"T{tenantId:N}"[..12],
            $"tenant-{tenantId:N}",
            "Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
        dbContext.Customers.Add(CustomerEntity.CreatePosCustomer(
            customerId,
            tenantId,
            $"C{customerId:N}"[..12],
            "Test Customer",
            "+94 77 123 4567",
            "customer@example.com",
            Guid.NewGuid(),
            Now));
        dbContext.CustomerAuthAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return new SeededAccount(customerId, account);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private sealed record SeededAccount(Guid CustomerId, CustomerAuthAccount Account);

    private sealed record SeededRefreshSession(
        Guid CustomerId,
        Guid SessionId,
        Guid RefreshTokenId,
        Guid TokenFamilyId);
}
