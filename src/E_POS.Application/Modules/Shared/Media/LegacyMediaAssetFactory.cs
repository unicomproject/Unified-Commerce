using System.Security.Cryptography;
using System.Text;
using E_POS.Domain.Modules.Shared.Media.Entities;

namespace E_POS.Application.Modules.Shared.Media;

public static class LegacyMediaAssetFactory
{
    private const string AssetTypeImage = "IMAGE";
    private const string ActiveStatus = "ACTIVE";
    private const string LegacyContainerName = "legacy-media";
    private const string LegacyOriginalFileName = "legacy-image";

    public static MediaAsset? CreateImageFromUrl(
        Guid tenantId,
        Guid ownerId,
        string ownerSegment,
        string assetPurpose,
        string? publicUrl,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        Guid? mediaAssetId = null)
    {
        var normalizedUrl = NormalizeUrl(publicUrl);
        if (normalizedUrl is null)
        {
            return null;
        }

        var id = mediaAssetId ?? Guid.NewGuid();
        var extension = InferSupportedImageExtension(normalizedUrl);
        var storageKey = BuildLegacyStorageKey(ownerSegment, ownerId, id, extension);

        return MediaAsset.Create(
            id,
            tenantId,
            LegacyContainerName,
            storageKey,
            normalizedUrl,
            ResolveOriginalFileName(normalizedUrl, extension),
            ResolveMimeType(extension),
            extension,
            fileSizeBytes: 1,
            widthPx: null,
            heightPx: null,
            checksumHash: ComputeChecksum(normalizedUrl),
            AssetTypeImage,
            NormalizePurpose(assetPurpose),
            ActiveStatus,
            createdByTenantUserId,
            now);
    }

    private static string? NormalizeUrl(string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return null;
        }

        var value = publicUrl.Trim();
        return value.Length == 0 ? null : value;
    }

    private static string BuildLegacyStorageKey(
        string ownerSegment,
        Guid ownerId,
        Guid mediaAssetId,
        string extension)
    {
        return string.Join(
            '/',
            "legacy",
            SanitizeSegment(ownerSegment),
            ownerId.ToString("D"),
            $"{mediaAssetId:D}{extension}");
    }

    private static string SanitizeSegment(string value)
    {
        var normalized = value.Trim().Trim('/').Replace('\\', '/');
        return string.IsNullOrWhiteSpace(normalized) ? "media" : normalized;
    }

    private static string InferSupportedImageExtension(string publicUrl)
    {
        var extension = TryGetPathExtension(publicUrl);
        return extension switch
        {
            ".jpeg" => ".jpg",
            ".jpg" or ".png" or ".webp" => extension,
            _ => ".jpg"
        };
    }

    private static string ResolveMimeType(string extension)
    {
        return extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static string ResolveOriginalFileName(string publicUrl, string extension)
    {
        var fileName = TryGetPathFileName(publicUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"{LegacyOriginalFileName}{extension}";
        }

        fileName = fileName.Trim();
        return fileName.Length <= 255 ? fileName : fileName[..255];
    }

    private static string? TryGetPathExtension(string publicUrl)
    {
        var path = TryGetUrlPath(publicUrl);
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetExtension(path).ToLowerInvariant();
    }

    private static string? TryGetPathFileName(string publicUrl)
    {
        var path = TryGetUrlPath(publicUrl);
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFileName(path);
    }

    private static string? TryGetUrlPath(string publicUrl)
    {
        if (Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath;
        }

        var queryIndex = publicUrl.IndexOf('?', StringComparison.Ordinal);
        return queryIndex >= 0 ? publicUrl[..queryIndex] : publicUrl;
    }

    private static string NormalizePurpose(string purpose)
    {
        var value = string.IsNullOrWhiteSpace(purpose) ? "IMAGE" : purpose.Trim();
        value = value.Replace(' ', '_').Replace('-', '_').ToUpperInvariant();
        return value.Length <= 80 ? value : value[..80];
    }

    private static string ComputeChecksum(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
