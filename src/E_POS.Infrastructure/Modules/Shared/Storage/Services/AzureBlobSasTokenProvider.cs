using System;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using E_POS.Application.Modules.Shared.Storage.Contracts;
using E_POS.Infrastructure.Modules.Shared.Storage.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Shared.Storage.Services;

public sealed class AzureBlobSasTokenProvider : IAzureSasTokenProvider
{
    private readonly AzureBlobStorageOptions _options;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobSasTokenProvider(IOptions<AzureBlobStorageOptions> options)
    {
        _options = options.Value;
        
        if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        }
        else
        {
            throw new InvalidOperationException("AzureBlobStorage ConnectionString is not configured.");
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
            // Extract blob name from URL based on the configured public base URL
            if (!blobUrl.StartsWith(_options.PublicBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                // If it doesn't match the expected base URL, just return it as is
                return blobUrl;
            }

            string relativePath = blobUrl.Substring(_options.PublicBaseUrl.Length).TrimStart('/');
            
            // Expected format: containerName/blobName
            string[] parts = relativePath.Split('/', 2);
            if (parts.Length != 2)
            {
                return blobUrl;
            }

            string containerName = parts[0];
            string blobName = parts[1];

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                return blobUrl;
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
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
}
