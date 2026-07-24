using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Email;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Integrations.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformPasswordResetLinkBuilder : IPlatformPasswordResetLinkBuilder
{
    private readonly PlatformPasswordResetSettings _settings;

    public PlatformPasswordResetLinkBuilder(PlatformPasswordResetSettings settings)
    {
        _settings = settings;
    }

    public string BuildResetUrl(string rawToken)
    {
        var baseUrl = (_settings.PublicAppBaseUrl ?? string.Empty).TrimEnd('/');
        var path = string.IsNullOrWhiteSpace(_settings.ResetPath)
            ? "/reset-password"
            : _settings.ResetPath.StartsWith('/')
                ? _settings.ResetPath
                : "/" + _settings.ResetPath;

        return $"{baseUrl}{path}?token={Uri.EscapeDataString(rawToken)}";
    }
}

/// <summary>
/// Delivers platform password-reset links via ACS email when configured.
/// Development may fall back to admin_secure_link when ACS is not configured
/// and <see cref="AzureCommunicationEmailOptions.AllowAdminSecureLinkFallback"/> is true.
/// Never logs the raw token or reset URL query token.
/// </summary>
public sealed class AcsPlatformPasswordResetDeliveryService : IPlatformPasswordResetDeliveryService
{
    private static readonly ApplicationError EmailNotConfigured = new(
        "platform_password_reset.email_not_configured",
        "Password reset email delivery is not configured.");

    private readonly IApplicationEmailSender _emailSender;
    private readonly AzureCommunicationEmailOptions _emailOptions;
    private readonly ILogger<AcsPlatformPasswordResetDeliveryService> _logger;

    public AcsPlatformPasswordResetDeliveryService(
        IApplicationEmailSender emailSender,
        IOptions<AzureCommunicationEmailOptions> emailOptions,
        ILogger<AcsPlatformPasswordResetDeliveryService> logger)
    {
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<ApplicationResult<PlatformPasswordResetDeliveryResult>> DeliverAsync(
        PlatformPasswordResetDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_emailSender.IsConfigured)
        {
            if (_emailOptions.AllowAdminSecureLinkFallback)
            {
                _logger.LogInformation(
                    "ACS email not configured; returning admin_secure_link for platform user {PlatformUserId}; expires at {ExpiresAt}. Raw token not logged.",
                    request.PlatformUserId,
                    request.ExpiresAt);

                return ApplicationResult<PlatformPasswordResetDeliveryResult>.Success(
                    new PlatformPasswordResetDeliveryResult(
                        PlatformPasswordResetConstants.DeliveryModeAdminSecureLink,
                        request.ResetUrl,
                        "Password reset link created. Share the secure link with the user out of band until email delivery is configured."));
            }

            _logger.LogWarning(
                "ACS email not configured and admin_secure_link fallback is disabled. Platform user {PlatformUserId}.",
                request.PlatformUserId);

            return ApplicationResult<PlatformPasswordResetDeliveryResult>.Failure(EmailNotConfigured);
        }

        var message = PlatformPasswordResetEmailComposer.Compose(
            request.Email,
            request.DisplayName,
            request.ResetUrl,
            request.ExpiresAt,
            correlationId: request.PlatformUserId.ToString("D"));

        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        if (sendResult.IsFailure || sendResult.Value is null)
        {
            _logger.LogWarning(
                "Platform password reset email failed for user {PlatformUserId}. ErrorCode={ErrorCode}",
                request.PlatformUserId,
                sendResult.Error.Code);

            return ApplicationResult<PlatformPasswordResetDeliveryResult>.Failure(
                sendResult.IsFailure
                    ? sendResult.Error
                    : new ApplicationError(
                        "email.provider_failed",
                        "Email provider send failed."));
        }

        _logger.LogInformation(
            "Platform password reset email accepted for user {PlatformUserId}; expires at {ExpiresAt}; OperationId={OperationId}. Raw token not logged.",
            request.PlatformUserId,
            request.ExpiresAt,
            sendResult.Value.OperationId);

        return ApplicationResult<PlatformPasswordResetDeliveryResult>.Success(
            new PlatformPasswordResetDeliveryResult(
                PlatformPasswordResetConstants.DeliveryModeEmail,
                ResetUrlForAdmin: null,
                Message: "Password reset email has been sent to the user."));
    }
}

/// <summary>
/// Legacy Release 1 delivery: returns the reset URL only to the authorized admin response path.
/// Retained for tests and explicit fallback scenarios; production DI uses
/// <see cref="AcsPlatformPasswordResetDeliveryService"/>.
/// </summary>
public sealed class AdminSecureLinkPasswordResetDeliveryService : IPlatformPasswordResetDeliveryService
{
    private readonly ILogger<AdminSecureLinkPasswordResetDeliveryService> _logger;

    public AdminSecureLinkPasswordResetDeliveryService(
        ILogger<AdminSecureLinkPasswordResetDeliveryService> logger)
    {
        _logger = logger;
    }

    public Task<ApplicationResult<PlatformPasswordResetDeliveryResult>> DeliverAsync(
        PlatformPasswordResetDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Platform password reset link prepared for user {PlatformUserId}; expires at {ExpiresAt}. Raw token not logged.",
            request.PlatformUserId,
            request.ExpiresAt);

        return Task.FromResult(ApplicationResult<PlatformPasswordResetDeliveryResult>.Success(
            new PlatformPasswordResetDeliveryResult(
                PlatformPasswordResetConstants.DeliveryModeAdminSecureLink,
                request.ResetUrl,
                "Password reset link created. Share the secure link with the user out of band until email delivery is configured.")));
    }
}
