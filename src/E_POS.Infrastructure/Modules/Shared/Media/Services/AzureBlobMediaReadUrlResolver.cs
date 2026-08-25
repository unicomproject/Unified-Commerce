using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Storage.Contracts;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Media.Services;

public sealed class AzureBlobMediaReadUrlResolver : IMediaReadUrlResolver
{
    private readonly IAzureSasTokenProvider _sasTokenProvider;
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobMediaReadUrlResolver(
        IAzureSasTokenProvider sasTokenProvider,
        IOptions<AzureBlobStorageOptions> options)
    {
        _sasTokenProvider = sasTokenProvider;
        _options = options.Value;
    }

    public string? ResolveReadUrl(string? mediaPublicUrl)
    {
        return ResolveReadUrl(null, null, mediaPublicUrl);
    }

    public string? ResolveReadUrl(
        string? containerName,
        string? storageKey,
        string? mediaPublicUrl)
    {
        var candidateUrl = FirstAbsoluteHttpUrl(mediaPublicUrl)
            ?? BuildBlobUrl(containerName, storageKey)
            ?? FirstNonEmpty(mediaPublicUrl, null);
        return string.IsNullOrWhiteSpace(candidateUrl)
            ? null
            : _sasTokenProvider.AppendReadSasToken(candidateUrl);
    }

    private string? BuildBlobUrl(string? containerName, string? storageKey)
    {
        var normalizedStorageKey = NormalizePath(storageKey);
        if (string.IsNullOrWhiteSpace(normalizedStorageKey) ||
            string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return null;
        }

        if (Uri.TryCreate(normalizedStorageKey, UriKind.Absolute, out _))
        {
            return normalizedStorageKey;
        }

        var normalizedContainerName = NormalizePath(containerName);
        var baseUrl = _options.PublicBaseUrl.Trim().TrimEnd('/');
        var includeContainer = !string.IsNullOrWhiteSpace(normalizedContainerName) &&
                               !normalizedStorageKey.StartsWith(
                                   $"{normalizedContainerName}/",
                                   StringComparison.OrdinalIgnoreCase) &&
                               !BaseUrlAlreadyEndsWithContainer(baseUrl, normalizedContainerName);
        var relativePath = includeContainer
            ? $"{normalizedContainerName}/{normalizedStorageKey}"
            : normalizedStorageKey;

        return $"{baseUrl}/{relativePath}";
    }

    private static bool BaseUrlAlreadyEndsWithContainer(string baseUrl, string containerName)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = NormalizePath(uri.AbsolutePath);
        return string.Equals(path.Split('/').LastOrDefault(), containerName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first)
            ? first.Trim()
            : string.IsNullOrWhiteSpace(second) ? null : second.Trim();
    }

    private static string? FirstAbsoluteHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Trim();
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? candidate
            : null;
    }

    private static string NormalizePath(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace('\\', '/').Trim('/');
    }
}
