using E_POS.Application.Modules.Tenant.TenantAuth;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Integration;

public sealed class TenantOnboardingOutboxOptionsValidator : IValidateOptions<TenantOnboardingOutboxOptions>
{
    private readonly IHostEnvironment _environment;

    public TenantOnboardingOutboxOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, TenantOnboardingOutboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var requireHttps = _environment.IsProduction();
        if (requireHttps)
        {
            if (!TenantAdminInvitationUrlBuilder.TryValidateBaseUrl(
                    options.TenantAdminAppBaseUrl,
                    requireHttps: true,
                    out var error))
            {
                return ValidateOptionsResult.Fail(error ?? "Invalid TenantAdminAppBaseUrl.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(options.TenantAdminAppBaseUrl) &&
                 !TenantAdminInvitationUrlBuilder.TryValidateBaseUrl(
                     options.TenantAdminAppBaseUrl,
                     requireHttps: false,
                     out var nonProdError))
        {
            return ValidateOptionsResult.Fail(nonProdError ?? "Invalid TenantAdminAppBaseUrl.");
        }

        return ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Production must not start without ACS sender configuration (no silent fake/console fallback).
/// </summary>
public sealed class ProductionAzureCommunicationEmailOptionsValidator
    : IValidateOptions<Integrations.Email.AzureCommunicationEmailOptions>
{
    private readonly IHostEnvironment _environment;

    public ProductionAzureCommunicationEmailOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, Integrations.Email.AzureCommunicationEmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!_environment.IsProduction())
        {
            return ValidateOptionsResult.Success;
        }

        var hasConnectionString = !string.IsNullOrWhiteSpace(options.ConnectionString);
        var hasEndpoint = !string.IsNullOrWhiteSpace(options.Endpoint);
        if (!hasConnectionString && !hasEndpoint)
        {
            return ValidateOptionsResult.Fail(
                "AzureCommunicationEmail: ConnectionString or Endpoint is required in Production.");
        }

        if (string.IsNullOrWhiteSpace(options.SenderAddress))
        {
            return ValidateOptionsResult.Fail(
                "AzureCommunicationEmail:SenderAddress is required in Production.");
        }

        return ValidateOptionsResult.Success;
    }
}
