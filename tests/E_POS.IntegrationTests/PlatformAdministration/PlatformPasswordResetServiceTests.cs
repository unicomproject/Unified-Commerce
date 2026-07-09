using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformPasswordResetServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 14, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    private static readonly Guid PlatformUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task CreatePendingResetToken_WritesPendingRequestedAtAndExpiresAt()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var created = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal(Now.AddHours(PlatformPasswordResetConstants.DefaultLifetimeHours), created.Value!.ExpiresAt);

        var saved = await dbContext.PlatformPasswordResetTokens.SingleAsync();
        Assert.Equal(PlatformAuthConstants.PendingTokenStatus, saved.Status);
        Assert.Equal(Now, saved.RequestedAt);
        Assert.Equal(Now.AddHours(PlatformPasswordResetConstants.DefaultLifetimeHours), saved.ExpiresAt);
        Assert.Equal(PlatformUserId, saved.PlatformUserId);
    }

    [Fact]
    public async Task CreatePendingResetToken_DoesNotStoreRawToken()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var created = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var saved = await dbContext.PlatformPasswordResetTokens.SingleAsync();
        Assert.NotEqual(created.Value!.RawToken, saved.TokenHash);
        Assert.DoesNotContain(created.Value.RawToken, saved.TokenHash, StringComparison.Ordinal);
        Assert.False(dbContext.PlatformPasswordResetTokens.Any(x => x.TokenHash == created.Value.RawToken));
    }

    [Fact]
    public async Task ValidateResetToken_SucceedsForPendingUnexpiredToken()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var created = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var validated = await service.ValidateResetTokenAsync(created.Value!.RawToken, CancellationToken.None);

        Assert.True(validated.IsSuccess);
        Assert.Equal(created.Value.TokenId, validated.Value!.TokenId);
        Assert.Equal(PlatformUserId, validated.Value.PlatformUserId);
    }

    [Fact]
    public async Task ValidateResetToken_FailsForUsedToken()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var created = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var marked = await service.MarkTokenUsedAsync(created.Value!.RawToken, CancellationToken.None);
        Assert.True(marked.IsSuccess);

        var validated = await service.ValidateResetTokenAsync(created.Value.RawToken, CancellationToken.None);
        Assert.True(validated.IsFailure);
        Assert.Equal("platform_password_reset.token_used", validated.Error.Code);
    }

    [Fact]
    public async Task ValidateResetToken_FailsForRevokedToken()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var created = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var revoked = await service.RevokeActivePendingTokensAsync(PlatformUserId, CancellationToken.None);
        Assert.True(revoked.IsSuccess);
        Assert.Equal(1, revoked.Value);

        var validated = await service.ValidateResetTokenAsync(created.Value!.RawToken, CancellationToken.None);
        Assert.True(validated.IsFailure);
        Assert.Equal("platform_password_reset.token_revoked", validated.Error.Code);
    }

    [Fact]
    public async Task ValidateResetToken_FailsForExpiredToken()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var createService = CreateService(dbContext, Now);

        var created = await createService.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var validateService = CreateService(dbContext, Now.AddHours(2));
        var validated = await validateService.ValidateResetTokenAsync(
            created.Value!.RawToken,
            CancellationToken.None);

        Assert.True(validated.IsFailure);
        Assert.Equal("platform_password_reset.token_expired", validated.Error.Code);
    }

    [Fact]
    public async Task ValidateResetToken_FailsForExpiredStatusLegacyRow()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var tokenHash = new TokenHashService().HashToken("legacy-expired-token", JwtSettings.SigningKey);
        var legacyToken = PlatformPasswordResetToken.CreateLegacy(
            Guid.NewGuid(),
            PlatformUserId,
            tokenHash,
            PlatformAuthConstants.ExpiredTokenStatus,
            Now.AddHours(-3));
        legacyToken.ApplyAlignmentBackfill();
        legacyToken.ApplyHardeningBackfill();
        dbContext.PlatformPasswordResetTokens.Add(legacyToken);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var validated = await service.ValidateResetTokenAsync("legacy-expired-token", CancellationToken.None);

        Assert.True(validated.IsFailure);
        Assert.Equal("platform_password_reset.token_expired", validated.Error.Code);
    }

    [Fact]
    public async Task RevokeActivePendingTokens_OnlyAffectsPendingTokens()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var pendingOne = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        var pendingTwo = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(pendingOne.IsSuccess);
        Assert.True(pendingTwo.IsSuccess);

        var used = await service.MarkTokenUsedAsync(pendingOne.Value!.RawToken, CancellationToken.None);
        Assert.True(used.IsSuccess);

        var revokedCount = await service.RevokeActivePendingTokensAsync(PlatformUserId, CancellationToken.None);
        Assert.True(revokedCount.IsSuccess);
        Assert.Equal(1, revokedCount.Value);

        var tokens = await dbContext.PlatformPasswordResetTokens.ToListAsync();

        var usedToken = tokens.Single(x => x.Id == pendingOne.Value!.TokenId);
        var revokedToken = tokens.Single(x => x.Id == pendingTwo.Value!.TokenId);

        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, usedToken.Status);
        Assert.Null(usedToken.RevokedAt);
        Assert.Equal(PlatformAuthConstants.RevokedTokenStatus, revokedToken.Status);
        Assert.Equal(Now, revokedToken.RevokedAt);
    }

    [Fact]
    public async Task MarkTokenUsed_WritesUsedStatusAndUsedAt()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext);
        var service = CreateService(dbContext);

        var created = await service.CreatePendingResetTokenAsync(PlatformUserId, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var marked = await service.MarkTokenUsedAsync(created.Value!.RawToken, CancellationToken.None);
        Assert.True(marked.IsSuccess);

        var saved = await dbContext.PlatformPasswordResetTokens.SingleAsync();
        Assert.Equal(PlatformAuthConstants.UsedTokenStatus, saved.Status);
        Assert.Equal(Now, saved.UsedAt);
        Assert.Null(saved.RevokedAt);
    }

    private static void SeedUser(EPosDbContext dbContext)
    {
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            PlatformUserId,
            "admin@nytroz.local",
            "hash",
            PlatformAuthConstants.ActiveStatus,
            Now));
        dbContext.SaveChanges();
    }

    private static PlatformPasswordResetService CreateService(
        EPosDbContext dbContext,
        DateTimeOffset? utcNow = null)
    {
        return new PlatformPasswordResetService(
            new PlatformPasswordResetRepository(dbContext),
            new RefreshTokenGenerator(new FixedDateTimeProvider(utcNow ?? Now)),
            new TokenHashService(),
            new FixedDateTimeProvider(utcNow ?? Now),
            JwtSettings);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
