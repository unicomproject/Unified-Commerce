using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Integrations.Email;

public sealed class AzureCommunicationEmailOptionsValidator : IValidateOptions<AzureCommunicationEmailOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureCommunicationEmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var hasConnectionString = !string.IsNullOrWhiteSpace(options.ConnectionString);
        var hasEndpoint = !string.IsNullOrWhiteSpace(options.Endpoint);

        if (!hasConnectionString && !hasEndpoint)
        {
            // ACS disabled — delivery may fall back in Development when AllowAdminSecureLinkFallback is true.
            return ValidateOptionsResult.Success;
        }

        if (hasConnectionString && hasEndpoint)
        {
            return ValidateOptionsResult.Fail(
                "AzureCommunicationEmail: configure either ConnectionString or Endpoint, not both.");
        }

        if (string.IsNullOrWhiteSpace(options.SenderAddress))
        {
            return ValidateOptionsResult.Fail(
                "AzureCommunicationEmail:SenderAddress is required when ConnectionString or Endpoint is configured.");
        }

        if (hasEndpoint &&
            (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpointUri) ||
             (endpointUri.Scheme != Uri.UriSchemeHttps && endpointUri.Scheme != Uri.UriSchemeHttp)))
        {
            return ValidateOptionsResult.Fail(
                "AzureCommunicationEmail:Endpoint must be an absolute http(s) URI.");
        }

        return ValidateOptionsResult.Success;
    }
}
