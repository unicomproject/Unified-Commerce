using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerPasswordResetLinkBuilder : ICustomerPasswordResetLinkBuilder
{
    private readonly CustomerPasswordResetSettings _settings;

    public CustomerPasswordResetLinkBuilder(CustomerPasswordResetSettings settings)
    {
        _settings = settings;
    }

    public string BuildResetUrl(string email, string rawToken)
    {
        var baseUrl = (_settings.PublicStorefrontBaseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:4200";
        }

        var path = string.IsNullOrWhiteSpace(_settings.ResetPath)
            ? "/reset-password"
            : _settings.ResetPath.StartsWith('/')
                ? _settings.ResetPath
                : "/" + _settings.ResetPath;

        var query = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";
        return $"{baseUrl}{path}?{query}";
    }
}