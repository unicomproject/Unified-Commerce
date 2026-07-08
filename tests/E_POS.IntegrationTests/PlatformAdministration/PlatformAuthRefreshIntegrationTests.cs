using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformAuthRefreshIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    [Fact]
    public async Task RefreshAsync_AfterLogin_ReturnsNewAccessTokenAndMarksOldRefreshTokenUsed()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        var authRepository = new PlatformAuthRepository(
            dbContext,
            new PlatformPermissionRepository(dbContext));
        var service = CreateService(authRepository);

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(login.IsSuccess);

        var refresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);

        Assert.True(refresh.IsSuccess);
        Assert.NotEqual(login.Value.AccessToken, refresh.Value!.AccessToken);
        Assert.Equal(36, refresh.Value.Permissions.Count);

        var usedTokenCount = await dbContext.PlatformRefreshTokens.CountAsync(
            x => x.Status == PlatformAuthConstants.UsedTokenStatus);
        var activeTokenCount = await dbContext.PlatformRefreshTokens.CountAsync(
            x => x.Status == PlatformAuthConstants.ActiveTokenStatus);

        Assert.Equal(1, usedTokenCount);
        Assert.Equal(1, activeTokenCount);
    }

    [Fact]
    public async Task RefreshAsync_WithReusedRefreshToken_ReturnsUnauthorized()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        var authRepository = new PlatformAuthRepository(
            dbContext,
            new PlatformPermissionRepository(dbContext));
        var service = CreateService(authRepository);

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        var firstRefresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(firstRefresh.IsSuccess);

        var reusedRefresh = await service.RefreshAsync(login.Value.RefreshToken, CancellationToken.None);

        Assert.True(reusedRefresh.IsFailure);
        Assert.Equal("platform_auth.refresh_token_reused", reusedRefresh.Error.Code);
    }

    [Fact]
    public async Task LogoutAsync_AfterRefresh_RevokesCurrentSession()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        var authRepository = new PlatformAuthRepository(
            dbContext,
            new PlatformPermissionRepository(dbContext));
        var service = CreateService(authRepository);

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        var refresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(refresh.IsSuccess);

        var sessionId = (await dbContext.PlatformAuthSessions.SingleAsync()).Id;
        var logout = await service.LogoutAsync(
            PlatformAdminSeedConstants.DevelopmentPlatformUserId,
            sessionId,
            CancellationToken.None);

        Assert.True(logout.IsSuccess);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
    }

    [Fact]
    public async Task LoginAsync_WithMigrationSeedPasswordHash_ReturnsSuccess()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        var authRepository = new PlatformAuthRepository(
            dbContext,
            new PlatformPermissionRepository(dbContext));
        var service = CreateService(authRepository);

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(login.IsSuccess);
        Assert.Equal(36, login.Value!.Permissions.Count);
    }

    [Fact]
    public async Task LogoutByRefreshTokenAsync_WithActiveRefreshToken_RevokesSession()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);

        var authRepository = new PlatformAuthRepository(
            dbContext,
            new PlatformPermissionRepository(dbContext));
        var service = CreateService(authRepository);

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        await service.LogoutByRefreshTokenAsync(login.Value!.RefreshToken, CancellationToken.None);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
    }

    private static async Task SeedSuperAdminAsync(EPosDbContext dbContext)
    {
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            PlatformAdminSeedConstants.DevelopmentPlatformUserId,
            DevelopmentPlatformAdminSeedData.Email,
            DevelopmentPlatformAdminSeedData.PasswordHash,
            PlatformAuthConstants.ActiveStatus,
            Now));

        await dbContext.SaveChangesAsync();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);
    }

    private static PlatformAuthService CreateService(PlatformAuthRepository authRepository)
    {
        return new PlatformAuthService(
            authRepository,
            new PlatformAuthRequestValidator(),
            new PasswordHashService(),
            new JwtTokenFactory(new SystemDateTimeProvider()),
            new RefreshTokenGenerator(new SystemDateTimeProvider()),
            new TokenHashService(),
            new SystemDateTimeProvider(),
            JwtSettings);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}



