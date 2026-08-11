using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Media.Services;

public sealed class AzureBlobMediaObjectStorage : IMediaObjectStorage
{
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobMediaObjectStorage(IOptions<AzureBlobStorageOptions> options)
    {
        _options = options.Value;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ConnectionString) &&
        !string.IsNullOrWhiteSpace(_options.ContainerName);

    public async Task<MediaObjectUploadResult> UploadAsync(
        MediaObjectUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var containerName = _options.ContainerName.Trim();
        var storageKey = request.StorageKey.Trim().Replace('\\', '/');

        try
        {
            var containerClient = new BlobContainerClient(_options.ConnectionString, containerName);
            if (_options.CreateContainerIfNotExists)
            {
                await containerClient.CreateIfNotExistsAsync(
                    PublicAccessType.None,
                    cancellationToken: cancellationToken);
            }

<<<<<<< HEAD
            var blobClient = containerClient.GetBlobClient(storageKey);
            if (request.Content.CanSeek)
            {
                request.Content.Position = 0;
            }

            await blobClient.UploadAsync(
                request.Content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = request.ContentType
                    },
                    Metadata = request.Metadata.ToDictionary(
                        x => x.Key,
                        x => x.Value,
                        StringComparer.Ordinal)
                },
                cancellationToken);

            return new MediaObjectUploadResult(
                containerName,
                storageKey,
                ResolvePublicUrl(blobClient.Uri, containerName, storageKey));
        }
        catch (Exception)
        {
            return await SaveToLocalStorageAsync(containerName, storageKey, request.Content, cancellationToken);
        }
    }

    public async Task DeleteIfExistsAsync(
        string containerName,
        string storageKey,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return;
        }

        try
        {
            var containerClient = new BlobContainerClient(_options.ConnectionString, containerName.Trim());
            var blobClient = containerClient.GetBlobClient(storageKey.Trim().Replace('\\', '/'));
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            // Best-effort cleanup
        }

        DeleteFromLocalStorage(containerName, storageKey);
    }

    private static async Task<MediaObjectUploadResult> SaveToLocalStorageAsync(
        string containerName,
        string storageKey,
        Stream content,
        CancellationToken cancellationToken)
    {
        var localStorageRoot = Path.Combine(AppContext.BaseDirectory, "App_Data", "media-storage", containerName);
        var localPath = Path.Combine(localStorageRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using var fileStream = File.Create(localPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        var publicUrl = $"/uploads/{containerName}/{storageKey}";
        return new MediaObjectUploadResult(containerName, storageKey, publicUrl);
    }

    private static void DeleteFromLocalStorage(string containerName, string storageKey)
    {
        try
        {
            var localStorageRoot = Path.Combine(AppContext.BaseDirectory, "App_Data", "media-storage", containerName);
            var localPath = Path.Combine(localStorageRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
        }
        catch
        {
            // Best effort
        }
    }

    private string ResolvePublicUrl(Uri blobUri, string containerName, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return blobUri.ToString();
        }

        var normalizedStorageKey = storageKey.Trim().Replace('\\', '/').Trim('/');
        var normalizedContainerName = containerName.Trim().Replace('\\', '/').Trim('/');
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

        var path = uri.AbsolutePath.Trim('/').Replace('\\', '/');
        return string.Equals(path.Split('/').LastOrDefault(), containerName, StringComparison.OrdinalIgnoreCase);
    }
}
