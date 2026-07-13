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
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

/// <summary>
/// Phase 8G-c: verifies refresh-token partial unique indexes on PostgreSQL.
/// </summary>
public sealed class PlatformAuthRefreshTokenChainIndexTests
{
    private const string PostgreSqlConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 7, 9, 11, 30, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    [Fact]
    public async Task Login_CreatesSingleActiveRefreshToken_OnPostgreSql()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var (userId, email) = await SeedIsolatedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(login.IsSuccess);
        Assert.Equal(1, await dbContext.PlatformRefreshTokens.CountAsync(
            x => x.PlatformUserId == userId && x.Status == PlatformAuthConstants.ActiveTokenStatus));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task RefreshRotation_SucceedsWithPartialUniqueIndexes_OnPostgreSql()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var (userId, email) = await SeedIsolatedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        Assert.True(login.IsSuccess);

        var originalToken = await dbContext.PlatformRefreshTokens.SingleAsync(
            x => x.PlatformUserId == userId && x.Status == PlatformAuthConstants.ActiveTokenStatus);
        var refresh = await service.RefreshAsync(login.Value!.RefreshToken, CancellationToken.None);
        Assert.True(refresh.IsSuccess);

        var usedToken = await dbContext.PlatformRefreshTokens.SingleAsync(x => x.Id == originalToken.Id);
        var activeToken = await dbContext.PlatformRefreshTokens.SingleAsync(
            x => x.PlatformUserId == userId && x.Status == PlatformAuthConstants.ActiveTokenStatus);

        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, usedToken.Status);
        Assert.Equal(activeToken.Id, usedToken.ReplacedByTokenId);
        Assert.Equal(originalToken.TokenFamilyId, activeToken.TokenFamilyId);
        Assert.Equal(1, await dbContext.PlatformRefreshTokens.CountAsync(
            x => x.PlatformUserId == userId && x.Status == PlatformAuthConstants.ActiveTokenStatus));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task MultipleLogins_AllowMultipleActiveSessionsAndTokensForSameUser_OnPostgreSql()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var (userId, email) = await SeedIsolatedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var loginOne = await service.LoginAsync(
            new PlatformAdminLoginRequest(email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        var loginTwo = await service.LoginAsync(
            new PlatformAdminLoginRequest(email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(loginOne.IsSuccess);
        Assert.True(loginTwo.IsSuccess);

        var activeSessions = await dbContext.PlatformAuthSessions
            .CountAsync(x => x.PlatformUserId == userId && x.Status == PlatformAuthConstants.ActiveTokenStatus);
        var activeTokens = await dbContext.PlatformRefreshTokens
            .CountAsync(x => x.PlatformUserId == userId && x.Status == PlatformAuthConstants.ActiveTokenStatus);

        Assert.Equal(2, activeSessions);
        Assert.Equal(2, activeTokens);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task DuplicateActiveRefreshTokenForSameSession_ThrowsOnPostgreSql()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var familyOne = Guid.NewGuid();
        var familyTwo = Guid.NewGuid();

        dbContext.PlatformUsers.Add(PlatformUser.Create(
            userId,
            $"chain-index-{userId:N}@example.test",
            "HASHED:test-password",
            PlatformAuthConstants.ActiveStatus,
            Now));

        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(
            sessionId,
            userId,
            CreateTokenHash("session"),
            Now));

        dbContext.PlatformRefreshTokens.Add(PlatformRefreshToken.Create(
            familyOne,
            sessionId,
            CreateTokenHash("refresh-one"),
            Now.AddDays(7),
            Now,
            platformUserId: userId,
            tokenFamilyId: familyOne));

        await dbContext.SaveChangesAsync();

        dbContext.PlatformRefreshTokens.Add(PlatformRefreshToken.Create(
            familyTwo,
            sessionId,
            CreateTokenHash("refresh-two"),
            Now.AddDays(7),
            Now,
            platformUserId: userId,
            tokenFamilyId: familyTwo));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        Assert.Contains("uq_platform_refresh_tokens_platform_auth_session_id_active", exception.InnerException?.Message ?? exception.Message, StringComparison.OrdinalIgnoreCase);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task DuplicateActiveRefreshTokenForSameFamily_ThrowsOnPostgreSql()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        var sessionOne = Guid.NewGuid();
        var sessionTwo = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        dbContext.PlatformUsers.Add(PlatformUser.Create(
            userId,
            $"family-index-{userId:N}@example.test",
            "HASHED:test-password",
            PlatformAuthConstants.ActiveStatus,
            Now));

        dbContext.PlatformAuthSessions.AddRange(
            PlatformAuthSession.Create(sessionOne, userId, CreateTokenHash("session-one"), Now),
            PlatformAuthSession.Create(sessionTwo, userId, CreateTokenHash("session-two"), Now));

        dbContext.PlatformRefreshTokens.Add(PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionOne,
            CreateTokenHash("refresh-one"),
            Now.AddDays(7),
            Now,
            platformUserId: userId,
            tokenFamilyId: familyId));

        await dbContext.SaveChangesAsync();

        dbContext.PlatformRefreshTokens.Add(PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionTwo,
            CreateTokenHash("refresh-two"),
            Now.AddDays(7),
            Now,
            platformUserId: userId,
            tokenFamilyId: familyId));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        Assert.Contains("uq_platform_refresh_tokens_token_family_id_active", exception.InnerException?.Message ?? exception.Message, StringComparison.OrdinalIgnoreCase);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Login_CreatesSingleActiveRefreshToken_PerSession()
    {
        await using var dbContext = CreateInMemoryDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var login = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(login.IsSuccess);
        Assert.Equal(1, await dbContext.PlatformRefreshTokens.CountAsync(x => x.Status == PlatformAuthConstants.ActiveTokenStatus));
    }

    [Fact]
    public async Task MultipleActiveSessions_AllowedForSamePlatformUser_OnInMemory()
    {
        await using var dbContext = CreateInMemoryDbContext();
        await SeedSuperAdminAsync(dbContext);
        var service = CreateService(new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)));

        var loginOne = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);
        var loginTwo = await service.LoginAsync(
            new PlatformAdminLoginRequest(DevelopmentPlatformAdminSeedData.Email, DevelopmentPlatformAdminSeedData.Password),
            CancellationToken.None);

        Assert.True(loginOne.IsSuccess);
        Assert.True(loginTwo.IsSuccess);
        Assert.Equal(2, await dbContext.PlatformAuthSessions.CountAsync(x => x.Status == PlatformAuthConstants.ActiveTokenStatus));
        Assert.Equal(2, await dbContext.PlatformRefreshTokens.CountAsync(x => x.Status == PlatformAuthConstants.ActiveTokenStatus));
    }

    private static async Task<bool> CanConnectToPostgreSqlAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(PostgreSqlConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (PostgresException)
        {
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    private static async Task<(Guid UserId, string Email)> SeedIsolatedSuperAdminAsync(EPosDbContext dbContext)
    {
        var userId = Guid.NewGuid();
        var email = $"pg-chain-index-{userId:N}@example.test";

        dbContext.PlatformUsers.Add(PlatformUser.Create(
            userId,
            email,
            DevelopmentPlatformAdminSeedData.PasswordHash,
            PlatformAuthConstants.ActiveStatus,
            Now));

        await dbContext.SaveChangesAsync();
        await PlatformAdminPermissionSeedApplicator.ApplyAsync(dbContext, Now);

        dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
            Guid.NewGuid(),
            userId,
            PlatformAdminSeedConstants.SuperAdministratorRoleId,
            "PostgreSQL refresh-chain index test role assignment.",
            Now));

        await dbContext.SaveChangesAsync();

        return (userId, email);
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

    private static EPosDbContext CreatePostgreSqlDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(PostgreSqlConnectionString)
            .Options;

        return new EPosDbContext(options);
    }

    private static EPosDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private static string CreateTokenHash(string label) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{label}:{Guid.NewGuid():N}"));

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
