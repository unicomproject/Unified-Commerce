using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

/// <summary>
/// Phase 8D: verifies refresh token family chain alignment during login and rotation.
/// </summary>
public sealed class PlatformAuthRefreshTokenChainTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 13, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    [Fact]
    public async Task Login_CreatesRefreshTokenWithTokenFamilyIdEqualToOwnId()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(login.IsSuccess);

        var refreshToken = await dbContext.PlatformRefreshTokens.SingleAsync();
        Assert.Equal(refreshToken.Id, refreshToken.TokenFamilyId);
    }

    [Fact]
    public async Task Login_CreatesRefreshTokenWithPlatformUserIdPopulated()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(login.IsSuccess);

        var refreshToken = await dbContext.PlatformRefreshTokens.SingleAsync();
        Assert.Equal(PlatformAdminSeedConstants.DevelopmentPlatformUserId, refreshToken.PlatformUserId);
    }

    [Fact]
    public async Task RefreshRotation_SetsOldTokenUsedAtAndReplacedByTokenId()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        var originalToken = await dbContext.PlatformRefreshTokens.SingleAsync();
        var refresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(refresh.IsSuccess);

        var usedToken = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Id == originalToken.Id);
        var activeToken = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Status == PlatformAuthConstants.ActiveTokenStatus);

        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, usedToken.Status);
        Assert.Equal(Now, usedToken.UsedAt);
        Assert.Equal(activeToken.Id, usedToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task RefreshRotation_NewTokenInheritsSameTokenFamilyId()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        var originalToken = await dbContext.PlatformRefreshTokens.SingleAsync();
        var refresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(refresh.IsSuccess);

        var activeToken = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Status == PlatformAuthConstants.ActiveTokenStatus);
        Assert.Equal(originalToken.TokenFamilyId, activeToken.TokenFamilyId);
        Assert.Equal(originalToken.Id, activeToken.TokenFamilyId);
    }

    [Fact]
    public async Task RefreshRotation_NewTokenHasPlatformUserIdPopulated()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        var refresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(refresh.IsSuccess);

        var activeToken = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Status == PlatformAuthConstants.ActiveTokenStatus);
        Assert.Equal(PlatformAdminSeedConstants.DevelopmentPlatformUserId, activeToken.PlatformUserId);
    }

    [Fact]
    public async Task RefreshTokenReuse_BehaviorRemainsUnchanged()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        var firstRefresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(firstRefresh.IsSuccess);

        var reusedRefresh = await service.RefreshAsync(login.Value.RefreshToken, CancellationToken.None);
        Assert.True(reusedRefresh.IsFailure);
        Assert.Equal("platform_auth.refresh_token_reused", reusedRefresh.Error.Code);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.RefreshTokenReuse, session.RevokeReason);

        var activeTokenCount = await dbContext.PlatformRefreshTokens.CountAsync(
            x => x.Status == PlatformAuthConstants.ActiveTokenStatus);
        Assert.Equal(0, activeTokenCount);
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
            new JwtTokenFactory(new FixedDateTimeProvider()),
            new RefreshTokenGenerator(new FixedDateTimeProvider()),
            new TokenHashService(),
            new FixedDateTimeProvider(),
            JwtSettings);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
