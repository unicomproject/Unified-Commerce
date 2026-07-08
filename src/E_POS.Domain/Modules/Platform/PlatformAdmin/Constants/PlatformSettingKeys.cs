namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

public static class PlatformSettingKeys
{
    public const string PlatformDisplayName = "general.platform_display_name";
    public const string SupportEmail = "general.support_email";
    public const string DefaultCountryCode = "general.default_country_code";
    public const string DefaultCurrencyCode = "general.default_currency_code";
    public const string DefaultTimezone = "general.default_timezone";
    public const string DefaultLocale = "general.default_locale";

    public static readonly IReadOnlyList<string> GeneralSettings =
    [
        PlatformDisplayName,
        SupportEmail,
        DefaultCountryCode,
        DefaultCurrencyCode,
        DefaultTimezone,
        DefaultLocale
    ];
}

