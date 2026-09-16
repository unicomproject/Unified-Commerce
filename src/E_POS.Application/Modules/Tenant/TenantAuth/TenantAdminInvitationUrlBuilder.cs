namespace E_POS.Application.Modules.Tenant.TenantAuth;

/// <summary>
/// Builds Tenant Admin invitation setup URLs. Canonical path matches Flutter:
/// <c>/tenant-admin/setup/{token}</c>
/// </summary>
public static class TenantAdminInvitationUrlBuilder
{
    public const string CanonicalSetupPath = "/tenant-admin/setup";
    public const string DevelopmentNativeBaseUrl = "oneverz://tenant-admin/setup";

    public static string Build(string baseUrl, string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var trimmed = baseUrl.Trim().TrimEnd('/');
        if (string.Equals(baseUrl.Trim(), DevelopmentNativeBaseUrl, StringComparison.Ordinal))
            return $"{DevelopmentNativeBaseUrl}?token={Uri.EscapeDataString(rawToken)}";
        return $"{trimmed}{CanonicalSetupPath}/{Uri.EscapeDataString(rawToken)}";
    }

    public static bool TryValidateBaseUrl(string? baseUrl, bool requireHttps, out string? error)
    {
        error = null;
        // Only this explicit native endpoint is accepted outside Production.
        // Production validation below continues to require a real HTTPS host.
        if (!requireHttps && string.Equals(baseUrl?.Trim(), DevelopmentNativeBaseUrl, StringComparison.Ordinal))
            return true;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            error = "TenantOnboardingOutbox:TenantAdminAppBaseUrl is required.";
            return false;
        }

        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            error = "TenantOnboardingOutbox:TenantAdminAppBaseUrl must be an absolute URI.";
            return false;
        }

        if (requireHttps)
        {
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                error = "TenantOnboardingOutbox:TenantAdminAppBaseUrl must use https in Production.";
                return false;
            }

            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase))
            {
                error = "TenantOnboardingOutbox:TenantAdminAppBaseUrl must not use localhost in Production.";
                return false;
            }
        }
        else if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            error = "TenantOnboardingOutbox:TenantAdminAppBaseUrl must be an http(s) URI.";
            return false;
        }

        return true;
    }
}
