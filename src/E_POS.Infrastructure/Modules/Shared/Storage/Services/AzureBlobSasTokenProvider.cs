using System;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using E_POS.Application.Modules.Shared.Storage.Contracts;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Storage.Services;

public sealed class AzureBlobSasTokenProvider : IAzureSasTokenProvider
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string? _storageAccountName;

    public AzureBlobSasTokenProvider(IOptions<AzureBlobStorageOptions> options)
    {
        var configuredOptions = options.Value;

        if (!string.IsNullOrWhiteSpace(configuredOptions.ConnectionString))
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(configuredOptions.ConnectionString);
                _storageAccountName = TryParseStorageAccountName(_blobServiceClient.Uri);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("AzureBlobStorage:ConnectionString is malformed or uses a placeholder. Please configure a valid connection string using .NET User Secrets or an environment variable.");
            }
        }
    }

    public string AppendReadSasToken(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            return blobUrl;
        }

        try
        {
            var normalizedBlobUrl = blobUrl.Trim();
            if (_blobServiceClient is null ||
                !Uri.TryCreate(normalizedBlobUrl, UriKind.Absolute, out var blobUri) ||
                HasSasQuery(blobUri) ||
                !IsConfiguredStorageAccount(blobUri))
            {
                return normalizedBlobUrl;
            }

            var blobUriBuilder = new BlobUriBuilder(blobUri);
            if (string.IsNullOrWhiteSpace(blobUriBuilder.BlobContainerName) ||
                string.IsNullOrWhiteSpace(blobUriBuilder.BlobName))
            {
                return normalizedBlobUrl;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
            var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

            if (!blobClient.CanGenerateSasUri)
            {
                return normalizedBlobUrl;
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobUriBuilder.BlobContainerName,
                BlobName = blobUriBuilder.BlobName,
                Resource = "b", // b for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Prevent clock skew issues
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(24) // 24 hours expiry
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            
            return sasUri.ToString();
        }
        catch (Exception)
        {
            // In case of any error parsing the URL or generating SAS, fallback to original URL
            return blobUrl;
        }
    }

    private bool IsConfiguredStorageAccount(Uri blobUri)
    {
        if (!blobUri.Host.Contains(".blob.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_storageAccountName))
        {
            return true;
        }

        var accountName = TryParseStorageAccountName(blobUri);
        return string.Equals(accountName, _storageAccountName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSasQuery(Uri uri)
    {
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var keys = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2)[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return keys.Contains("sig") && keys.Contains("se") && keys.Contains("sp");
    }

    private static string? TryParseStorageAccountName(Uri uri)
    {
        var hostParts = uri.Host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return hostParts.Length == 0 ? null : hostParts[0];
    }
}
