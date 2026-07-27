using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
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
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformPasswordResetEmailFlowUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 10, 0, 0, TimeSpan.Zero);
    private static readonly PlatformJwtSettings JwtSettings = new(
        "TM-EPOS",
        "TM-EPOS-Platform",
        "TEST_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
        15,
        7);

    private static readonly Guid ActorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TargetId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Initiate_UsesEmailAbstraction_AndDoesNotExposeResetUrl()
    {
        await using var dbContext = CreateDbContext();
        var passwordHashService = new PasswordHashService();
        SeedUsers(dbContext, passwordHashService);
        await dbContext.SaveChangesAsync();

        var emailSender = new RecordingEmailSender();
        var delivery = new AcsPlatformPasswordResetDeliveryService(
            emailSender,
            Options.Create(new AzureCommunicationEmailOptions
            {
                AllowAdminSecureLinkFallback = false,
                SenderAddress = "noreply@oneverz.local"
            }),
            NullLogger<AcsPlatformPasswordResetDeliveryService>.Instance);

        var service = CreateService(dbContext, passwordHashService, delivery);

        var initiated = await service.InitiateAdminPasswordResetAsync(
            TargetId,
            ActorId,
            new PlatformAuthClientContext("127.0.0.1", "test-agent", null),
            CancellationToken.None);

        Assert.True(initiated.IsSuccess);
        Assert.Equal(PlatformPasswordResetConstants.DeliveryModeEmail, initiated.Value!.DeliveryMode);
        Assert.Null(initiated.Value.ResetUrl);
        Assert.Single(emailSender.Sent);
        Assert.Equal("target@nytroz.local", emailSender.Sent[0].ToAddress);
        Assert.Contains("token=", emailSender.Sent[0].HtmlBody, StringComparison.Ordinal);

        var pending = await dbContext.PlatformPasswordResetTokens
            .Where(x => x.PlatformUserId == TargetId && x.Status == PlatformPasswordResetConstants.TokenStatus.Pending)
            .ToListAsync();
        Assert.Single(pending);
        Assert.DoesNotContain("token=", pending[0].TokenHash, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Initiate_WhenEmailFails_DoesNotMarkSuccessAuditAsCompletedPath()
    {
        await using var dbContext = CreateDbContext();
        var passwordHashService = new PasswordHashService();
        SeedUsers(dbContext, passwordHashService);
        await dbContext.SaveChangesAsync();

        var emailSender = new RecordingEmailSender(
            new ApplicationError("email.provider_failed", "Email provider rejected the send request."));
        var delivery = new AcsPlatformPasswordResetDeliveryService(
            emailSender,
            Options.Create(new AzureCommunicationEmailOptions
            {
                AllowAdminSecureLinkFallback = false,
                SenderAddress = "noreply@oneverz.local"
            }),
            NullLogger<AcsPlatformPasswordResetDeliveryService>.Instance);

        var service = CreateService(dbContext, passwordHashService, delivery);

        var initiated = await service.InitiateAdminPasswordResetAsync(
            TargetId,
            ActorId,
            null,
            CancellationToken.None);

        Assert.True(initiated.IsFailure);
        Assert.Equal("email.provider_failed", initiated.Error.Code);

        var audits = await dbContext.PlatformLoginAudits.ToListAsync();
        Assert.Contains(audits, x => x.AuthenticationMethod == PlatformPasswordResetConstants.AuditMethod.PasswordResetFailed);
        Assert.DoesNotContain(audits, x => x.AuthenticationMethod == PlatformPasswordResetConstants.AuditMethod.PasswordResetRequested);
    }

    [Fact]
    public void OptionsValidator_RequiresSender_WhenEndpointConfigured()
    {
        var validator = new AzureCommunicationEmailOptionsValidator();
        var result = validator.Validate(
            null,
            new AzureCommunicationEmailOptions
            {
                Endpoint = "https://example.communication.azure.com",
                SenderAddress = string.Empty
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("SenderAddress", StringComparison.Ordinal));
    }

    [Fact]
    public void OptionsValidator_AllowsEmpty_WhenAcsDisabled()
    {
        var validator = new AzureCommunicationEmailOptionsValidator();
        var result = validator.Validate(
            null,
            new AzureCommunicationEmailOptions
            {
                ConnectionString = string.Empty,
                Endpoint = string.Empty,
                SenderAddress = string.Empty
            });

        Assert.False(result.Failed);
    }

    private static void SeedUsers(EPosDbContext dbContext, IPasswordHashService passwordHashService)
    {
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            TargetId,
            "target@nytroz.local",
            passwordHashService.HashPassword("OldPass123"),
            PlatformAuthConstants.ActiveStatus,
            Now));
        dbContext.PlatformUsers.Add(PlatformUser.Create(
            ActorId,
            "actor@nytroz.local",
            passwordHashService.HashPassword("ActorPass123"),
            PlatformAuthConstants.ActiveStatus,
            Now));
    }

    private static PlatformPasswordResetService CreateService(
        EPosDbContext dbContext,
        IPasswordHashService passwordHashService,
        IPlatformPasswordResetDeliveryService delivery)
    {
        var dateTime = new FixedDateTimeProvider(Now);
        return new PlatformPasswordResetService(
            new PlatformPasswordResetRepository(dbContext),
            new PlatformUserRepository(dbContext),
            new PlatformAuthRepository(dbContext, new PlatformPermissionRepository(dbContext)),
            new AlwaysAllowPermissionChecker(),
            new RefreshTokenGenerator(dateTime),
            new TokenHashService(),
            passwordHashService,
            new PlatformPasswordPolicyValidator(),
            new FixedLinkBuilder(),
            delivery,
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

    private sealed class AlwaysAllowPermissionChecker : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FixedLinkBuilder : IPlatformPasswordResetLinkBuilder
    {
        public string BuildResetUrl(string rawToken)
            => $"https://admin.test/reset-password?token={Uri.EscapeDataString(rawToken)}";
    }

    private sealed class RecordingEmailSender : IApplicationEmailSender
    {
        private readonly ApplicationError? _failure;

        public RecordingEmailSender(ApplicationError? failure = null)
        {
            _failure = failure;
        }

        public bool IsConfigured => true;

        public List<ApplicationEmailMessage> Sent { get; } = [];

        public Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(
            ApplicationEmailMessage message,
            CancellationToken cancellationToken)
        {
            if (_failure is not null)
            {
                return Task.FromResult(ApplicationResult<ApplicationEmailSendResult>.Failure(_failure));
            }

            Sent.Add(message);
            return Task.FromResult(ApplicationResult<ApplicationEmailSendResult>.Success(
                new ApplicationEmailSendResult("op-1", "Started")));
        }
    }
}
