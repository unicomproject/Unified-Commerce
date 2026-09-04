using E_POS.Application.Common.Email;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Email;
using E_POS.Application.Modules.Tenant.TenantAuth;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

public sealed class TenantAdminInvitationDeliveryService : ITenantAdminInvitationDeliveryService
{
    private readonly IApplicationEmailSender _emailSender;
    private readonly TenantOnboardingOutboxOptions _options;
    private readonly ILogger<TenantAdminInvitationDeliveryService> _logger;

    public TenantAdminInvitationDeliveryService(
        IApplicationEmailSender emailSender,
        IOptions<TenantOnboardingOutboxOptions> options,
        ILogger<TenantAdminInvitationDeliveryService> logger)
    {
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TenantAdminInvitationDeliveryResult> DeliverAsync(
        TenantAdminInvitationDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var baseUrl = _options.TenantAdminAppBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("Tenant admin invitation delivery failed: BaseUrl not configured. TenantId={TenantId}", request.TenantId);
            return new TenantAdminInvitationDeliveryResult(false, "invitation_base_url_not_configured", "Tenant Admin application URL is not configured.");
        }

        if (!TenantAdminInvitationUrlBuilder.TryValidateBaseUrl(baseUrl, requireHttps: false, out var baseUrlError))
        {
            _logger.LogWarning("Tenant admin invitation delivery failed: BaseUrl invalid ({Error}). TenantId={TenantId}", baseUrlError, request.TenantId);
            return new TenantAdminInvitationDeliveryResult(false, "invitation_base_url_invalid", baseUrlError ?? "Tenant Admin application URL is invalid.");
        }

        if (!_emailSender.IsConfigured)
        {
            _logger.LogWarning("Tenant admin invitation email skipped: email provider not configured. TenantId={TenantId}", request.TenantId);
            return new TenantAdminInvitationDeliveryResult(false, "email.not_configured", "Email delivery is not configured.");
        }

        var activationUrl = TenantAdminInvitationUrlBuilder.Build(baseUrl, request.RawToken);
        var emailMessage = TenantAdminInvitationEmailComposer.Compose(
            toAddress: request.AdminEmail,
            tenantName: request.TenantName,
            tenantCode: request.TenantCode,
            adminUsername: request.AdminEmail,
            activationUrl: activationUrl,
            loginUrl: baseUrl,
            expiresAt: request.ExpiresAt,
            correlationId: request.CorrelationId);

        try
        {
            var sendResult = await _emailSender.SendAsync(emailMessage, cancellationToken);
            if (sendResult.IsSuccess)
            {
                _logger.LogInformation("Tenant admin invitation email delivered successfully to {Email} for tenant {TenantId}.", request.AdminEmail, request.TenantId);
                return new TenantAdminInvitationDeliveryResult(true);
            }

            _logger.LogWarning("Tenant admin invitation email provider rejected send to {Email} for tenant {TenantId}. Error: {Error}",
                request.AdminEmail, request.TenantId, sendResult.Error.Message);
            return new TenantAdminInvitationDeliveryResult(false, sendResult.Error.Code, sendResult.Error.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error delivering tenant admin invitation email to {Email} for tenant {TenantId}.", request.AdminEmail, request.TenantId);
            return new TenantAdminInvitationDeliveryResult(false, "email.delivery_failed", ex.Message);
        }
    }
}
