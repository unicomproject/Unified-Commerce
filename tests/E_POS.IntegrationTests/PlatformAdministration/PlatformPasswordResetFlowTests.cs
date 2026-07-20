using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
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
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformPasswordResetFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    private static readonly Guid ActorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TargetId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task CompleteFlow_ResetsPassword_RevokesSessions_AndRejectsReuse()
    {
        await using var dbContext = CreateDbContext();
        var passwordHashService = new PasswordHashService();
        var oldHash = passwordHashService.HashPassword("OldPass123");
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            TargetId,
            "target@nytroz.local",
            oldHash,
            PlatformAuthConstants.ActiveStatus,
            Now));
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            ActorId,
            "actor@nytroz.local",
            passwordHashService.HashPassword("ActorPass123"),
            PlatformAuthConstants.ActiveStatus,
            Now));

        var sessionId = Guid.NewGuid();
        dbContext.PlatformAuthSessions.Add(PlatformAuthSession.Create(sessionId, TargetId, "session-hash", Now));
        dbContext.PlatformRefreshTokens.Add(PlatformRefreshToken.Create(
            Guid.NewGuid(),
            sessionId,
            "refresh-hash",
            Now.AddDays(7),
            Now,
            TargetId));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, passwordHashService, allowPermission: true);

        var initiated = await service.InitiateAdminPasswordResetAsync(
            TargetId,
            ActorId,
            new PlatformAuthClientContext("127.0.0.1", "test-agent", null),
            CancellationToken.None);

        Assert.True(initiated.IsSuccess);
        Assert.Equal(PlatformPasswordResetConstants.DeliveryModeAdminSecureLink, initiated.Value!.DeliveryMode);
        Assert.False(string.IsNullOrWhiteSpace(initiated.Value.ResetUrl));

        var token = ExtractToken(initiated.Value.ResetUrl!);
        var validated = await service.ValidatePublicTokenAsync(token, CancellationToken.None);
        Assert.True(validated.IsSuccess);
        Assert.True(validated.Value!.IsValid);

        var completed = await service.CompletePasswordResetAsync(
            new CompletePlatformPasswordResetRequest(token, "NewPass123", "NewPass123"),
            new PlatformAuthClientContext("127.0.0.1", "test-agent", null),
            CancellationToken.None);

        Assert.True(completed.IsSuccess);

        var user = await dbContext.PlatformUsers.SingleAsync(x => x.Id == TargetId);
        Assert.True(passwordHashService.VerifyPassword("NewPass123", user.PasswordHash));
        Assert.False(passwordHashService.VerifyPassword("OldPass123", user.PasswordHash));

        var session = await dbContext.PlatformAuthSessions.SingleAsync(x => x.Id == sessionId);
        Assert.NotNull(session.RevokedAt);

        var refresh = await dbContext.PlatformRefreshTokens.SingleAsync();
        Assert.NotNull(refresh.RevokedAt);
        Assert.Equal(PlatformAuthAlignmentConstants.RevokeReason.PasswordReset, refresh.RevokeReason);

        var reused = await service.CompletePasswordResetAsync(
            new CompletePlatformPasswordResetRequest(token, "AnotherPass1", "AnotherPass1"),
            null,
            CancellationToken.None);
        Assert.True(reused.IsFailure);
        Assert.Equal("platform_password_reset.token_used", reused.Error.Code);

        var audits = await dbContext.PlatformLoginAudits.ToListAsync();
        Assert.Contains(audits, x => x.AuthenticationMethod == PlatformPasswordResetConstants.AuditMethod.PasswordResetRequested);
        Assert.Contains(audits, x => x.AuthenticationMethod == PlatformPasswordResetConstants.AuditMethod.PasswordResetCompleted);
        Assert.Contains(audits, x => x.AuthenticationMethod == PlatformPasswordResetConstants.AuditMethod.SessionsRevoked);
    }

    [Fact]
    public async Task Initiate_WithoutPermission_ReturnsAccessDenied()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            TargetId,
            "target@nytroz.local",
            "hash",
            PlatformAuthConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new PasswordHashService(), allowPermission: false);
        var result = await service.InitiateAdminPasswordResetAsync(
            TargetId,
            ActorId,
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task Initiate_ForInactiveUser_ReturnsInvalidState()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            TargetId,
            "target@nytroz.local",
            "hash",
            PlatformAuthConstants.InactiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new PasswordHashService(), allowPermission: true);
        var result = await service.InitiateAdminPasswordResetAsync(
            TargetId,
            ActorId,
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_password_reset.invalid_user_state", result.Error.Code);
    }

    [Fact]
    public async Task Initiate_RevokesPreviousPendingTokens()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            TargetId,
            "target@nytroz.local",
            "hash",
            PlatformAuthConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new PasswordHashService(), allowPermission: true);
        var first = await service.InitiateAdminPasswordResetAsync(TargetId, ActorId, null, CancellationToken.None);
        var second = await service.InitiateAdminPasswordResetAsync(TargetId, ActorId, null, CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var firstToken = ExtractToken(first.Value!.ResetUrl!);
        var validated = await service.ValidatePublicTokenAsync(firstToken, CancellationToken.None);
        Assert.True(validated.IsSuccess);
        Assert.False(validated.Value!.IsValid);
        Assert.Equal(PlatformPasswordResetConstants.TokenStatus.Revoked, validated.Value.Status);
    }

    private static string ExtractToken(string resetUrl)
    {
        const string marker = "token=";
        var index = resetUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Assert.True(index >= 0);
        var encoded = resetUrl[(index + marker.Length)..];
        var amp = encoded.IndexOf('&');
        if (amp >= 0)
        {
            encoded = encoded[..amp];
        }

        return Uri.UnescapeDataString(encoded);
    }

    private static PlatformPasswordResetService CreateService(
        EPosDbContext dbContext,
        IPasswordHashService passwordHashService,
        bool allowPermission)
    {
        var dateTime = new FixedDateTimeProvider(Now);
        return new PlatformPasswordResetService(
            new PlatformPasswordResetRepository(dbContext),
            new PlatformUserRepository(dbContext),
            new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)),
            new FixedPermissionChecker(allowPermission),
            new RefreshTokenGenerator(dateTime),
            new TokenHashService(),
            passwordHashService,
            new PlatformPasswordPolicyValidator(),
            new FixedLinkBuilder(),
            new PassthroughDeliveryService(),
            dateTime,
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

    private sealed class FixedPermissionChecker(bool allow) : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken)
            => Task.FromResult(allow);
    }

    private sealed class FixedLinkBuilder : IPlatformPasswordResetLinkBuilder
    {
        public string BuildResetUrl(string rawToken) => $"https://admin.test/reset-password?token={Uri.EscapeDataString(rawToken)}";
    }

    private sealed class PassthroughDeliveryService : IPlatformPasswordResetDeliveryService
    {
        public Task<PlatformPasswordResetDeliveryResult> DeliverAsync(
            PlatformPasswordResetDeliveryRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new PlatformPasswordResetDeliveryResult(
                PlatformPasswordResetConstants.DeliveryModeAdminSecureLink,
                request.ResetUrl,
                "ok"));
    }
}
