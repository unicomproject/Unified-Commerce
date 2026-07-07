using System.Net.Mail;
using System.Text.RegularExpressions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Validators;

public static partial class PlatformSettingsRequestValidator
{
    public static ApplicationError? ValidateUpdate(UpdatePlatformSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fieldErrors = new List<ApplicationFieldError>();

        ValidatePlatformDisplayName(fieldErrors, request.PlatformDisplayName);
        ValidateSupportEmail(fieldErrors, request.SupportEmail);
        ValidateCountryCode(fieldErrors, "defaultCountryCode", request.DefaultCountryCode);
        ValidateCurrencyCode(fieldErrors, "defaultCurrencyCode", request.DefaultCurrencyCode);
        ValidateTimezone(fieldErrors, request.DefaultTimezone);
        ValidateLocale(fieldErrors, request.DefaultLocale);

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return new ApplicationError(
            "platform_settings.validation_failed",
            "One or more platform settings fields are invalid.",
            fieldErrors);
    }

    private static void ValidatePlatformDisplayName(
        ICollection<ApplicationFieldError> fieldErrors,
        string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "platformDisplayName",
                "Platform display name is required."));
            return;
        }

        if (normalized.Length > 200)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "platformDisplayName",
                "Platform display name must be 200 characters or fewer."));
        }
    }

    private static void ValidateSupportEmail(
        ICollection<ApplicationFieldError> fieldErrors,
        string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (normalized.Length > 320)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "supportEmail",
                "Support email must be 320 characters or fewer."));
            return;
        }

        if (!MailAddress.TryCreate(normalized, out _))
        {
            fieldErrors.Add(new ApplicationFieldError("supportEmail", "Support email is invalid."));
        }
    }

    private static void ValidateCountryCode(
        ICollection<ApplicationFieldError> fieldErrors,
        string fieldName,
        string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!IsoCountryCodeRegex().IsMatch(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                fieldName,
                "Country code must be exactly 2 letters (for example LK)."));
        }
    }

    private static void ValidateCurrencyCode(
        ICollection<ApplicationFieldError> fieldErrors,
        string fieldName,
        string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!IsoCurrencyCodeRegex().IsMatch(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                fieldName,
                "Currency code must be exactly 3 letters (for example LKR)."));
        }
    }

    private static void ValidateTimezone(
        ICollection<ApplicationFieldError> fieldErrors,
        string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (normalized.Length > 100 || !TimezoneRegex().IsMatch(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "defaultTimezone",
                "Timezone must use a valid IANA identifier (for example Asia/Colombo)."));
        }
    }

    private static void ValidateLocale(
        ICollection<ApplicationFieldError> fieldErrors,
        string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (normalized.Length > 20 || !LocaleRegex().IsMatch(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "defaultLocale",
                "Locale must use a valid language tag (for example en-LK)."));
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    [GeneratedRegex("^[A-Za-z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoCountryCodeRegex();

    [GeneratedRegex("^[A-Za-z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoCurrencyCodeRegex();

    [GeneratedRegex("^[A-Za-z0-9_]+(?:/[A-Za-z0-9_+-]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex TimezoneRegex();

    [GeneratedRegex("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$", RegexOptions.CultureInvariant)]
    private static partial Regex LocaleRegex();
}

