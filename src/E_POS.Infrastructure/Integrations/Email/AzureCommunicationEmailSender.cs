using System.Net.Mail;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Integrations.Email;

/// <summary>
/// Thin transport over <see cref="EmailClient"/> so unit tests can assert send parameters
/// without calling Azure.
/// </summary>
internal interface IAcsEmailSendGateway
{
    string AuthMode { get; }

    Task<(string OperationId, string Status)> SendStartedAsync(
        string senderAddress,
        string recipientAddress,
        string subject,
        string htmlContent,
        string? plainTextContent,
        CancellationToken cancellationToken);
}

internal sealed class EmailClientAcsEmailSendGateway : IAcsEmailSendGateway
{
    private readonly EmailClient _emailClient;

    public EmailClientAcsEmailSendGateway(EmailClient emailClient, string authMode)
    {
        _emailClient = emailClient;
        AuthMode = authMode;
    }

    public string AuthMode { get; }

    public async Task<(string OperationId, string Status)> SendStartedAsync(
        string senderAddress,
        string recipientAddress,
        string subject,
        string htmlContent,
        string? plainTextContent,
        CancellationToken cancellationToken)
    {
        // Official simple overload: senderAddress must be the verified MailFrom email only.
        EmailSendOperation operation = await _emailClient.SendAsync(
            WaitUntil.Started,
            senderAddress,
            recipientAddress,
            subject,
            htmlContent,
            plainTextContent,
            cancellationToken);

        var operationId = operation.Id ?? string.Empty;
        var status = operation.HasValue
            ? operation.Value.Status.ToString()
            : "Started";

        return (operationId, status);
    }
}

public sealed class AzureCommunicationEmailSender : IApplicationEmailSender
{
    private static readonly ApplicationError NotConfigured = new(
        "email.not_configured",
        "Email delivery is not configured.");

    private static readonly ApplicationError InvalidMessage = new(
        "email.invalid_message",
        "Email message is invalid.");

    private readonly AzureCommunicationEmailOptions _options;
    private readonly IAcsEmailSendGateway? _gateway;
    private readonly ILogger<AzureCommunicationEmailSender> _logger;

    public AzureCommunicationEmailSender(
        IOptions<AzureCommunicationEmailOptions> options,
        ILogger<AzureCommunicationEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
        _gateway = CreateGateway(_options);
    }

    /// <summary>Test seam: inject a gateway without touching Azure.</summary>
    internal AzureCommunicationEmailSender(
        IOptions<AzureCommunicationEmailOptions> options,
        IAcsEmailSendGateway gateway,
        ILogger<AzureCommunicationEmailSender> logger)
    {
        _options = options.Value;
        _gateway = gateway;
        _logger = logger;
    }

    public bool IsConfigured =>
        _gateway is not null &&
        !string.IsNullOrWhiteSpace(_options.SenderAddress);

    public async Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(
        ApplicationEmailMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!IsConfigured || _gateway is null)
        {
            return ApplicationResult<ApplicationEmailSendResult>.Failure(NotConfigured);
        }

        var senderAddress = NormalizeAddress(_options.SenderAddress);
        var recipientAddress = NormalizeAddress(message.ToAddress);
        var subject = (message.Subject ?? string.Empty).Trim();
        var htmlContent = message.HtmlBody ?? string.Empty;
        var plainTextContent = string.IsNullOrWhiteSpace(message.PlainTextBody)
            ? null
            : message.PlainTextBody;

        if (!IsValidEmailAddress(senderAddress) ||
            !IsValidEmailAddress(recipientAddress) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning(
                "ACS email rejected locally before send. SenderValid={SenderValid}, RecipientValid={RecipientValid}, SubjectLength={SubjectLength}, HtmlLength={HtmlLength}, CorrelationId={CorrelationId}",
                IsValidEmailAddress(senderAddress),
                IsValidEmailAddress(recipientAddress),
                subject.Length,
                htmlContent.Length,
                message.CorrelationId);

            return ApplicationResult<ApplicationEmailSendResult>.Failure(InvalidMessage);
        }

        _logger.LogInformation(
            "ACS email send starting. AuthMode={AuthMode}, SenderDomain={SenderDomain}, RecipientDomain={RecipientDomain}, SubjectLength={SubjectLength}, HtmlLength={HtmlLength}, PlainTextLength={PlainTextLength}, CorrelationId={CorrelationId}",
            _gateway.AuthMode,
            ExtractDomain(senderAddress),
            ExtractDomain(recipientAddress),
            subject.Length,
            htmlContent.Length,
            plainTextContent?.Length ?? 0,
            message.CorrelationId);

        try
        {
            var (operationId, status) = await _gateway.SendStartedAsync(
                senderAddress,
                recipientAddress,
                subject,
                htmlContent,
                plainTextContent,
                cancellationToken);

            _logger.LogInformation(
                "ACS email send accepted. OperationId={OperationId}, Status={Status}, CorrelationId={CorrelationId}",
                operationId,
                status,
                message.CorrelationId);

            return ApplicationResult<ApplicationEmailSendResult>.Success(
                new ApplicationEmailSendResult(operationId, status, operationId));
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                "ACS email send failed. ErrorCode={ErrorCode}, Status={Status}, ProviderMessage={ProviderMessage}, ClientRequestId={ClientRequestId}, CorrelationId={CorrelationId}",
                ex.ErrorCode,
                ex.Status,
                ex.Message,
                TryGetClientRequestId(ex),
                message.CorrelationId);

            return ApplicationResult<ApplicationEmailSendResult>.Failure(new ApplicationError(
                "email.provider_failed",
                "Email provider rejected the send request."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "ACS email send failed unexpectedly. CorrelationId={CorrelationId}",
                message.CorrelationId);

            return ApplicationResult<ApplicationEmailSendResult>.Failure(new ApplicationError(
                "email.provider_failed",
                "Email provider send failed."));
        }
    }

    internal static string NormalizeAddress(string? value)
        => (value ?? string.Empty).Trim();

    internal static bool IsValidEmailAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address) ||
            address.Contains('<', StringComparison.Ordinal) ||
            address.Contains('>', StringComparison.Ordinal))
        {
            return false;
        }

        return MailAddress.TryCreate(address, out var parsed) &&
               string.Equals(parsed.Address, address, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ExtractDomain(string email)
    {
        var at = email.LastIndexOf('@');
        return at >= 0 && at < email.Length - 1
            ? email[(at + 1)..]
            : "(unknown)";
    }

    private static string? TryGetClientRequestId(RequestFailedException ex)
    {
        if (ex.Data is null)
        {
            return null;
        }

        foreach (var key in new[] { "ClientRequestId", "x-ms-client-request-id", "RequestId", "x-ms-request-id" })
        {
            if (ex.Data.Contains(key) && ex.Data[key] is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static IAcsEmailSendGateway? CreateGateway(AzureCommunicationEmailOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new EmailClientAcsEmailSendGateway(
                new EmailClient(options.ConnectionString),
                "connection_string");
        }

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return new EmailClientAcsEmailSendGateway(
                new EmailClient(new Uri(options.Endpoint), new DefaultAzureCredential()),
                "managed_identity");
        }

        return null;
    }
}
