using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Microsoft.Extensions.Logging;

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
/// Release 1 delivery: returns the reset URL only to the authorized admin response path.
/// Does not log the raw token. Production email wiring remains a documented gap.
/// </summary>
public sealed class AdminSecureLinkPasswordResetDeliveryService : IPlatformPasswordResetDeliveryService
{
    private readonly ILogger<AdminSecureLinkPasswordResetDeliveryService> _logger;

    public AdminSecureLinkPasswordResetDeliveryService(
        ILogger<AdminSecureLinkPasswordResetDeliveryService> logger)
    {
        _logger = logger;
    }

    public Task<PlatformPasswordResetDeliveryResult> DeliverAsync(
        PlatformPasswordResetDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Platform password reset link prepared for user {PlatformUserId}; expires at {ExpiresAt}. Raw token not logged.",
            request.PlatformUserId,
            request.ExpiresAt);

        return Task.FromResult(new PlatformPasswordResetDeliveryResult(
            PlatformPasswordResetConstants.DeliveryModeAdminSecureLink,
            request.ResetUrl,
            "Password reset link created. Share the secure link with the user out of band until email delivery is configured."));
    }
}
