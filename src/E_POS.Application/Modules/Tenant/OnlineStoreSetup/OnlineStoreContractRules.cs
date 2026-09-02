using System.Net.Mail;
using System.Globalization;
using System.Text.RegularExpressions;

namespace E_POS.Application.Modules.Tenant.OnlineStoreSetup;

public static partial class OnlineStoreContractRules
{
    public const int SlugMinLength = 3;
    public const int SlugMaxLength = 63;
    public const int StoreNameMaxLength = 150;
    public const int BusinessDisplayNameMaxLength = 150;
    public const int StoreDescriptionMaxLength = 2000;
    public const int StoreEmailMaxLength = 320;
    public const int StorePhoneMaxLength = 40;
    public const int SupportTaglineMaxLength = 160;
    public const int SupportHoursMaxLength = 500;
    public const int BusinessAddressMaxLength = 1000;

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> SupportedBrandingMediaFormats { get; } =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
            ["image/png"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png" },
            ["image/webp"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".webp" },
            ["image/svg+xml"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".svg" },
            ["image/x-icon"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".ico" }
        };

    public static IReadOnlySet<string> RequiredPolicyTypes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TERMS",
            "PRIVACY",
            "CANCELLATION",
            "COLLECTION"
        };

    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "app", "auth", "cdn", "checkout", "help", "mail",
        "oneverz", "pos", "shop", "static", "store", "support", "www"
    };

    public static string? NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = RepeatedHyphenRegex().Replace(value.Trim().ToLowerInvariant(), "-");
        if (normalized.Length is < SlugMinLength or > SlugMaxLength ||
            normalized[0] == '-' || normalized[^1] == '-' ||
            ReservedSlugs.Contains(normalized) ||
            normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            return null;
        }

        return normalized;
    }

    public static string? NormalizeDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized.Length > 253 || normalized.Contains("://", StringComparison.Ordinal) ||
            normalized.Contains('/') || normalized.Contains('?') || normalized.Contains('#') ||
            Uri.CheckHostName(normalized) != UriHostNameType.Dns || !normalized.Contains('.'))
        {
            return null;
        }

        return normalized;
    }

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Trim().Length <= StoreEmailMaxLength &&
        MailAddress.TryCreate(value.Trim(), out var address) &&
        string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool IsValidOptionalHttpsUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(uri.Host);

    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        var prefix = trimmed.StartsWith('+') ? "+" : string.Empty;
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length is >= 7 and <= 15 ? prefix + digits : null;
    }

    public static bool IsValidSupportHours(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > SupportHoursMaxLength) return false;
        var intervals = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (intervals.Length == 0) return false;

        foreach (var interval in intervals)
        {
            var match = SupportHoursRegex().Match(interval);
            if (!match.Success || !TryParseTime(match.Groups["open"].Value, out var open) ||
                !TryParseTime(match.Groups["close"].Value, out var close) || open >= close)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsSupportReady(
        string? email,
        string? phone,
        string? businessAddress,
        string? supportHours) =>
        IsValidEmail(email) &&
        NormalizePhone(phone) is not null &&
        !string.IsNullOrWhiteSpace(businessAddress) &&
        businessAddress.Trim().Length <= BusinessAddressMaxLength &&
        IsValidSupportHours(supportHours);

    public static int CountPublishedRequiredPolicies(IEnumerable<string> publishedPolicyTypes) =>
        publishedPolicyTypes
            .Where(policyType => RequiredPolicyTypes.Contains(policyType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    public static bool AreRequiredPoliciesPublished(IEnumerable<string> publishedPolicyTypes) =>
        CountPublishedRequiredPolicies(publishedPolicyTypes) == RequiredPolicyTypes.Count;

    public static bool ContainsUnsafeMarkup(string value) =>
        UnsafeMarkupRegex().IsMatch(value);

    public static bool IsSupportedBrandingMediaFormat(string mimeType, string extension) =>
        SupportedBrandingMediaFormats.TryGetValue(mimeType, out var extensions) &&
        extensions.Contains(extension);

    [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHyphenRegex();

    [GeneratedRegex("<\\s*script|<\\s*iframe|\\bon[a-z]+\\s*=|javascript\\s*:|data\\s*:\\s*text/html", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeMarkupRegex();

    private static bool TryParseTime(string value, out TimeOnly time)
    {
        var formats = new[] { "H:mm", "HH:mm", "h:mm tt", "hh:mm tt", "h tt", "hh tt" };
        return TimeOnly.TryParseExact(
            value.Trim(),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out time);
    }

    [GeneratedRegex("^(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun)(?:\\s*-\\s*(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun))?\\s*:\\s*(?<open>.+?)\\s*-\\s*(?<close>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SupportHoursRegex();
}
