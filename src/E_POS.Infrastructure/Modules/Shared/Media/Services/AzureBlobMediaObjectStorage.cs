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
        var containerClient = new BlobContainerClient(_options.ConnectionString, containerName);
        if (_options.CreateContainerIfNotExists)
        {
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken);
        }

        var storageKey = request.StorageKey.Trim().Replace('\\', '/');
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
            ResolvePublicUrl(blobClient.Uri, storageKey));
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

        var containerClient = new BlobContainerClient(_options.ConnectionString, containerName.Trim());
        var blobClient = containerClient.GetBlobClient(storageKey.Trim().Replace('\\', '/'));
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private string ResolvePublicUrl(Uri blobUri, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return blobUri.ToString();
        }

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";
    }
}
