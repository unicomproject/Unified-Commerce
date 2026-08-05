namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed record CustomerPasswordResetSettings(
    string PublicStorefrontBaseUrl,
    string ResetPath);
