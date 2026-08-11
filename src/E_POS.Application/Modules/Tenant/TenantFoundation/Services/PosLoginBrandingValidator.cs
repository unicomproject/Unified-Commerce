using System.Text.RegularExpressions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Domain.Modules.Shared.Media.Constants;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Services;

internal static partial class PosLoginBrandingValidator
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public static ApplicationError? ValidateText(string? value, int maxLength, bool required, string code, string label)
    {
        var normalized = value?.Trim();
        if (required && string.IsNullOrEmpty(normalized))
            return new ApplicationError(code, $"{label} is required.");
        if (normalized is { Length: > 0 } && normalized.Length > maxLength)
            return new ApplicationError(code, $"{label} must not exceed {maxLength} characters.");
        return null;
    }

    public static bool IsBackgroundMode(string value) => value is "IMAGE" or "COLOR";
    public static bool IsColor(string value) => ColorRegex().IsMatch(value);

    public static bool HasOnlyTenantNamePlaceholder(string value)
    {
        var withoutAllowed = value.Replace("{tenantName}", string.Empty, StringComparison.Ordinal);
        return !PlaceholderRegex().IsMatch(withoutAllowed);
    }

    public static ApplicationError? ValidateMedia(
        PosLoginBrandingMediaSnapshot? media,
        Guid tenantId,
        string requiredPurpose,
        string fieldCode)
    {
        if (media is null || media.TenantId != tenantId)
            return new ApplicationError(fieldCode, "The selected branding media is unavailable.");
        if (!string.Equals(media.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(media.AssetType, "IMAGE", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(media.AssetPurpose, requiredPurpose, StringComparison.OrdinalIgnoreCase) ||
            !ImageMimeTypes.Contains(media.MimeType) || media.FileSizeBytes <= 0 || media.FileSizeBytes > MaxImageBytes ||
            string.IsNullOrWhiteSpace(media.PublicUrl) || !ExtensionMatches(media.MimeType, media.FileExtension))
            return new ApplicationError(fieldCode, "The selected branding media is not usable.");
        return null;
    }

    public static bool IsEffectiveMedia(PosLoginBrandingMediaSnapshot? media, Guid tenantId, string purpose) =>
        ValidateMedia(media, tenantId, purpose, "branding.media_invalid") is null;

    private static bool ExtensionMatches(string mime, string extension) =>
        mime.ToLowerInvariant() switch
        {
            "image/jpeg" => extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase),
            "image/png" => extension.Equals(".png", StringComparison.OrdinalIgnoreCase),
            "image/webp" => extension.Equals(".webp", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    [GeneratedRegex("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();

    [GeneratedRegex("\\{[^{}]+\\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();
}
