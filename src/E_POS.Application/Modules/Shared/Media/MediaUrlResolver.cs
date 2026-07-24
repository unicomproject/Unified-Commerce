namespace E_POS.Application.Modules.Shared.Media;

public static class MediaUrlResolver
{
    public static string? PreferMediaAsset(
        string? mediaPublicUrl,
        string? legacyUrl,
        string? legacyStorageKey = null)
    {
        if (!string.IsNullOrWhiteSpace(mediaPublicUrl))
        {
            return mediaPublicUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(legacyUrl))
        {
            return legacyUrl.Trim();
        }

        return string.IsNullOrWhiteSpace(legacyStorageKey)
            ? null
            : legacyStorageKey.Trim();
    }

    public static string PreferMediaAssetOrEmpty(
        string? mediaPublicUrl,
        string? legacyUrl,
        string? legacyStorageKey = null)
    {
        return PreferMediaAsset(mediaPublicUrl, legacyUrl, legacyStorageKey) ?? string.Empty;
    }
}
