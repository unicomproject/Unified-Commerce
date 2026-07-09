using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
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
/// Phase 8C: verifies platform auth flows persist Second Brain metadata via domain dual-write primitives.
/// </summary>
public sealed class PlatformAuthFlowMetadataTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    private static readonly PlatformAuthClientContext ClientContext = new(
        "203.0.113.10",
        "Mozilla/5.0 PlatformAuthTest",
        null);

    [Fact]
    public async Task LoginSuccess_StoresSessionClientMetadata_WhenProvided()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None,
            ClientContext);

        Assert.True(login.IsSuccess);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        Assert.Equal(ClientContext.IpAddress, session.IpAddress);
        Assert.Equal(ClientContext.UserAgent, session.UserAgent);
        Assert.Null(session.DeviceName);
    }

    [Fact]
    public async Task LoginSuccess_AuditContainsSessionLinkAndAlignmentFields()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None,
            ClientContext);

        Assert.True(login.IsSuccess);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        var audit = await dbContext.PlatformLoginAudits.SingleAsync();

        Assert.Equal(session.Id, audit.PlatformAuthSessionId);
        Assert.Equal(PlatformAuthConstants.SuccessLoginResult, audit.LoginStatus);
        Assert.Equal(Now, audit.AttemptedAt);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, audit.AuthenticationMethod);
        Assert.Equal(ClientContext.IpAddress, audit.IpAddress);
        Assert.Equal(ClientContext.UserAgent, audit.UserAgent);
        Assert.Null(audit.FailureReason);
    }

    [Fact]
    public async Task LoginFailure_AuditStoresAlignmentFieldsAndFailureReason()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, "wrong-password"),
            CancellationToken.None,
            ClientContext);

        Assert.True(login.IsFailure);

        var audit = await dbContext.PlatformLoginAudits.SingleAsync();
        Assert.Equal(PlatformAuthConstants.FailedLoginResult, audit.LoginStatus);
        Assert.Equal(Now, audit.AttemptedAt);
        Assert.Equal(PlatformAuthAlignmentConstants.AuthenticationMethod.Password, audit.AuthenticationMethod);
        Assert.Equal(PlatformAuthAlignmentConstants.FailureReason.InvalidCredentials, audit.FailureReason);
        Assert.Equal(ClientContext.IpAddress, audit.IpAddress);
        Assert.Equal(ClientContext.UserAgent, audit.UserAgent);
        Assert.Null(audit.PlatformAuthSessionId);
    }

    [Fact]
    public async Task RefreshSuccess_SetsUsedAtOnOldTokenAndLastSeenAtOnSession()
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

        var usedToken = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Status == PlatformAuthConstants.UsedTokenStatus);
        var session = await dbContext.PlatformAuthSessions.SingleAsync();

        Assert.Equal(Now, usedToken.UsedAt);
        Assert.Equal(Now, session.LastSeenAt);
    }

    [Fact]
    public async Task Logout_SetsRevokedAtAndLogoutReasonOnSessionAndActiveRefreshTokens()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        var sessionId = (await dbContext.PlatformAuthSessions.SingleAsync()).Id;
        var logout = await service.LogoutAsync(
            PlatformAdminSeedConstants.DevelopmentPlatformUserId,
            sessionId,
            CancellationToken.None);
        Assert.True(logout.IsSuccess);

        var session = await dbContext.PlatformAuthSessions.SingleAsync();
        var refreshToken = await dbContext.PlatformRefreshTokens.SingleAsync();

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, session.Status);
        Assert.Equal(Now, session.RevokedAt);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.Logout, session.RevokeReason);
        Assert.Equal(PlatformAdminSeedConstants.DevelopmentPlatformUserId, session.RevokedByPlatformUserId);

        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, refreshToken.Status);
        Assert.Equal(Now, refreshToken.RevokedAt);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.Logout, refreshToken.RevokeReason);
    }

    [Fact]
    public async Task RefreshTokenReuse_SetsRevokedAtAndRefreshTokenReuseReason()
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
        Assert.Equal(Now, session.RevokedAt);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.RefreshTokenReuse, session.RevokeReason);
    }

    [Fact]
    public async Task LoginSuccess_ResponseShape_IsUnchanged()
    {
        await using var dbContext = CreateDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None,
            ClientContext);

        Assert.True(login.IsSuccess);
        Assert.NotNull(login.Value);
        Assert.False(string.IsNullOrWhiteSpace(login.Value!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.Value.RefreshToken));
        Assert.Equal(36, login.Value.Permissions.Count);
        Assert.Equal(PlatformAdminSeedConstants.DevelopmentPlatformUserId, login.Value.User.Id);
        Assert.Equal(DevelopmentPlatformAdminSeedData.Email, login.Value.User.Email);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, login.Value.User.Status);
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
