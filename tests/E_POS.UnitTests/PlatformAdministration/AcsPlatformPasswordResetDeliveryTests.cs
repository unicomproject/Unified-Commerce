using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Email;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class AcsPlatformPasswordResetDeliveryTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset ExpiresAt = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
    private const string ResetUrl = "https://admin.test/reset-password?token=raw-secret-token";

    [Fact]
    public async Task DeliverAsync_WhenEmailConfigured_SendsEmailAndHidesResetUrl()
    {
        var emailSender = new FakeEmailSender(configured: true);
        var sut = CreateSut(emailSender, allowFallback: false);

        var result = await sut.DeliverAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformPasswordResetConstants.DeliveryModeEmail, result.Value!.DeliveryMode);
        Assert.Null(result.Value.ResetUrlForAdmin);
        Assert.Single(emailSender.Sent);
        Assert.Equal("target@oneverz.local", emailSender.Sent[0].ToAddress);
        Assert.Equal(PlatformPasswordResetEmailComposer.Subject, emailSender.Sent[0].Subject);
        Assert.Contains(ResetUrl, emailSender.Sent[0].HtmlBody, StringComparison.Ordinal);
        Assert.Contains(ResetUrl, emailSender.Sent[0].PlainTextBody, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-secret-token", emailSender.LoggedMessages, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeliverAsync_WhenProviderFails_MapsFailure()
    {
        var emailSender = new FakeEmailSender(
            configured: true,
            failure: new ApplicationError("email.provider_failed", "Email provider rejected the send request."));
        var sut = CreateSut(emailSender, allowFallback: false);

        var result = await sut.DeliverAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.provider_failed", result.Error.Code);
    }

    [Fact]
    public async Task DeliverAsync_WhenNotConfigured_WithFallback_ReturnsAdminSecureLink()
    {
        var emailSender = new FakeEmailSender(configured: false);
        var sut = CreateSut(emailSender, allowFallback: true);

        var result = await sut.DeliverAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformPasswordResetConstants.DeliveryModeAdminSecureLink, result.Value!.DeliveryMode);
        Assert.Equal(ResetUrl, result.Value.ResetUrlForAdmin);
        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task DeliverAsync_WhenNotConfigured_WithoutFallback_Fails()
    {
        var emailSender = new FakeEmailSender(configured: false);
        var sut = CreateSut(emailSender, allowFallback: false);

        var result = await sut.DeliverAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_password_reset.email_not_configured", result.Error.Code);
    }

    [Fact]
    public void EmailComposer_HtmlEncodesDisplayName()
    {
        var message = PlatformPasswordResetEmailComposer.Compose(
            "user@oneverz.local",
            "<script>alert(1)</script>",
            ResetUrl,
            ExpiresAt);

        Assert.DoesNotContain("<script>", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains(ResetUrl, message.PlainTextBody!, StringComparison.Ordinal);
    }

    private static AcsPlatformPasswordResetDeliveryService CreateSut(
        FakeEmailSender emailSender,
        bool allowFallback)
    {
        var options = Options.Create(new AzureCommunicationEmailOptions
        {
            AllowAdminSecureLinkFallback = allowFallback,
            SenderAddress = allowFallback ? string.Empty : "noreply@oneverz.local",
            SenderDisplayName = "OneVerz"
        });

        return new AcsPlatformPasswordResetDeliveryService(
            emailSender,
            options,
            NullLogger<AcsPlatformPasswordResetDeliveryService>.Instance);
    }

    private static PlatformPasswordResetDeliveryRequest CreateRequest()
        => new(
            UserId,
            "target@oneverz.local",
            "Ada Lovelace",
            "raw-secret-token",
            ResetUrl,
            ExpiresAt);

    private sealed class FakeEmailSender : IApplicationEmailSender
    {
        private readonly ApplicationError? _failure;

        public FakeEmailSender(bool configured, ApplicationError? failure = null)
        {
            IsConfigured = configured;
            _failure = failure;
        }

        public bool IsConfigured { get; }

        public List<ApplicationEmailMessage> Sent { get; } = [];

        public string LoggedMessages { get; } = string.Empty;

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
                new ApplicationEmailSendResult("op-123", "Started", "op-123")));
        }
    }
}
