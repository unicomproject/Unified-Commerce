using Azure;
using E_POS.Application.Common.Email;
using E_POS.Infrastructure.Integrations.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class AzureCommunicationEmailSenderTests
{
    [Fact]
    public async Task SendAsync_TrimsSenderAndRecipient_AndDoesNotConcatenateDisplayName()
    {
        var gateway = new RecordingGateway();
        var sut = CreateSut(
            gateway,
            senderAddress: "  DoNotReply@contoso.azurecomm.net  ",
            senderDisplayName: "OneVerz");

        var result = await sut.SendAsync(
            new ApplicationEmailMessage(
                "  user@example.com  ",
                "  Reset your password  ",
                "<p>Hello</p>",
                "Hello",
                CorrelationId: "corr-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(gateway.LastCall);
        Assert.Equal("DoNotReply@contoso.azurecomm.net", gateway.LastCall!.SenderAddress);
        Assert.Equal("user@example.com", gateway.LastCall.RecipientAddress);
        Assert.Equal("Reset your password", gateway.LastCall.Subject);
        Assert.DoesNotContain("<", gateway.LastCall.SenderAddress, StringComparison.Ordinal);
        Assert.False(gateway.LastCall.SenderAddress.Contains("OneVerz", StringComparison.Ordinal));
        Assert.Equal("<p>Hello</p>", gateway.LastCall.HtmlContent);
        Assert.Equal("Hello", gateway.LastCall.PlainTextContent);
    }

    [Fact]
    public async Task SendAsync_RejectsDisplayNameFormattedSenderBeforeProviderCall()
    {
        var gateway = new RecordingGateway();
        var sut = CreateSut(
            gateway,
            senderAddress: "OneVerz <DoNotReply@contoso.azurecomm.net>",
            senderDisplayName: "OneVerz");

        var result = await sut.SendAsync(
            ValidMessage(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.invalid_message", result.Error.Code);
        Assert.Null(gateway.LastCall);
    }

    [Fact]
    public async Task SendAsync_RejectsEmptyRecipientBeforeProviderCall()
    {
        var gateway = new RecordingGateway();
        var sut = CreateSut(gateway, senderAddress: "DoNotReply@contoso.azurecomm.net");

        var result = await sut.SendAsync(
            new ApplicationEmailMessage("   ", "Subject", "<p>x</p>"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.invalid_message", result.Error.Code);
        Assert.Null(gateway.LastCall);
    }

    [Fact]
    public async Task SendAsync_RejectsInvalidRecipientBeforeProviderCall()
    {
        var gateway = new RecordingGateway();
        var sut = CreateSut(gateway, senderAddress: "DoNotReply@contoso.azurecomm.net");

        var result = await sut.SendAsync(
            new ApplicationEmailMessage("not-an-email", "Subject", "<p>x</p>"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.invalid_message", result.Error.Code);
        Assert.Null(gateway.LastCall);
    }

    [Fact]
    public async Task SendAsync_OnAzureBadRequest_MapsProviderFailed_AndDoesNotLogSecrets()
    {
        var logger = new CapturingLogger();
        var gateway = new RecordingGateway
        {
            ExceptionToThrow = new RequestFailedException(
                400,
                "Invalid sender address format.",
                "BadRequest",
                null)
        };
        var sut = new AzureCommunicationEmailSender(
            Options.Create(new AzureCommunicationEmailOptions
            {
                SenderAddress = "DoNotReply@contoso.azurecomm.net",
                SenderDisplayName = "OneVerz",
                ConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=REDACTED"
            }),
            gateway,
            logger);

        var result = await sut.SendAsync(
            new ApplicationEmailMessage(
                "user@example.com",
                "Reset your OneVerz password",
                "<a href=\"https://admin.test/reset-password?token=raw-secret-token\">Reset</a>",
                "https://admin.test/reset-password?token=raw-secret-token",
                CorrelationId: "corr-bad"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.provider_failed", result.Error.Code);

        var joined = string.Join('\n', logger.Messages);
        Assert.Contains("BadRequest", joined, StringComparison.Ordinal);
        Assert.Contains("400", joined, StringComparison.Ordinal);
        Assert.Contains("Invalid sender address format.", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-secret-token", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("reset-password?token=", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("accesskey", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("REDACTED", joined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_OnStatusSucceeded_ReturnsSuccess_AndPreservesOperationId()
    {
        var gateway = new RecordingGateway
        {
            StatusToReturn = "Succeeded",
            IsSuccessToReturn = true
        };
        var sut = CreateSut(gateway, senderAddress: "DoNotReply@contoso.azurecomm.net");

        var result = await sut.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("op-1", result.Value.OperationId);
        Assert.Equal("Succeeded", result.Value.Status);
    }

    [Fact]
    public async Task SendAsync_OnStatusStarted_ReturnsFailure_BecauseStartedIsProgressNotSuccess()
    {
        var gateway = new RecordingGateway
        {
            StatusToReturn = "Started",
            IsSuccessToReturn = false
        };
        var sut = CreateSut(gateway, senderAddress: "DoNotReply@contoso.azurecomm.net");

        var result = await sut.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.provider_failed", result.Error.Code);
        Assert.Contains("Status: Started", result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_OnStatusFailed_ReturnsFailure_AndLogsOperationDetails()
    {
        var gateway = new RecordingGateway
        {
            StatusToReturn = "Failed",
            IsSuccessToReturn = false,
            ErrorCodeToReturn = "InvalidAddress",
            ErrorMessageToReturn = "The recipient address is invalid."
        };
        var sut = CreateSut(gateway, senderAddress: "DoNotReply@contoso.azurecomm.net");

        var result = await sut.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.provider_failed", result.Error.Code);
        Assert.Contains("Status: Failed", result.Error.Message);
        Assert.Contains("ErrorCode: InvalidAddress", result.Error.Message);
        Assert.Contains("Message: The recipient address is invalid.", result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_OnStatusCanceled_ReturnsFailure_AndLogsCancellation()
    {
        var gateway = new RecordingGateway
        {
            StatusToReturn = "Canceled",
            IsSuccessToReturn = false
        };
        var sut = CreateSut(gateway, senderAddress: "DoNotReply@contoso.azurecomm.net");

        var result = await sut.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("email.provider_failed", result.Error.Code);
        Assert.Contains("Status: Canceled", result.Error.Message);
    }

    [Fact]
    public void NormalizeAndValidate_Helpers_TrimAndRejectAngleBrackets()
    {
        Assert.Equal(
            "DoNotReply@contoso.azurecomm.net",
            AzureCommunicationEmailSender.NormalizeAddress("  DoNotReply@contoso.azurecomm.net  "));
        Assert.True(AzureCommunicationEmailSender.IsValidEmailAddress("DoNotReply@contoso.azurecomm.net"));
        Assert.False(AzureCommunicationEmailSender.IsValidEmailAddress("OneVerz <DoNotReply@contoso.azurecomm.net>"));
        Assert.Equal("contoso.azurecomm.net", AzureCommunicationEmailSender.ExtractDomain("DoNotReply@contoso.azurecomm.net"));
    }

    private static AzureCommunicationEmailSender CreateSut(
        RecordingGateway gateway,
        string senderAddress,
        string senderDisplayName = "OneVerz")
    {
        return new AzureCommunicationEmailSender(
            Options.Create(new AzureCommunicationEmailOptions
            {
                SenderAddress = senderAddress,
                SenderDisplayName = senderDisplayName,
                ConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=REDACTED"
            }),
            gateway,
            new CapturingLogger());
    }

    private static ApplicationEmailMessage ValidMessage()
        => new("user@example.com", "Subject", "<p>body</p>", "body", "corr");

    private sealed class RecordingGateway : IAcsEmailSendGateway
    {
        public string AuthMode => "connection_string";

        public RequestFailedException? ExceptionToThrow { get; init; }

        public string StatusToReturn { get; set; } = "Succeeded";
        public bool IsSuccessToReturn { get; set; } = true;
        public string? ErrorCodeToReturn { get; set; }
        public string? ErrorMessageToReturn { get; set; }

        public SendCall? LastCall { get; private set; }

        public Task<AcsSendResult> SendAsync(
            string senderAddress,
            string recipientAddress,
            string subject,
            string htmlContent,
            string? plainTextContent,
            CancellationToken cancellationToken)
        {
            LastCall = new SendCall(senderAddress, recipientAddress, subject, htmlContent, plainTextContent);
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(new AcsSendResult(
                "op-1",
                StatusToReturn,
                IsSuccessToReturn,
                ErrorCodeToReturn,
                ErrorMessageToReturn));
        }
    }

    private sealed record SendCall(
        string SenderAddress,
        string RecipientAddress,
        string Subject,
        string HtmlContent,
        string? PlainTextContent);

    private sealed class CapturingLogger : ILogger<AzureCommunicationEmailSender>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null)
            {
                Messages.Add(exception.Message);
            }
        }
    }
}
