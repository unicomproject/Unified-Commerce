using System.Net;
using System.Net.Sockets;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Integrations.ProductLookup;

/// <summary>
/// Bounded HTTP fetch for external product image candidates.
/// Blocks non-http(s) schemes and private/loopback destinations (basic SSRF guard).
/// </summary>
public sealed class ExternalImageCandidateFetcher : IExternalImageCandidateFetcher
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalImageCandidateFetcher> _logger;

    public ExternalImageCandidateFetcher(
        HttpClient httpClient,
        ILogger<ExternalImageCandidateFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        if (_httpClient.Timeout == Timeout.InfiniteTimeSpan || _httpClient.Timeout > FetchTimeout)
        {
            _httpClient.Timeout = FetchTimeout;
        }
    }

    public async Task<ApplicationResult<MediaUploadFile>> FetchAsync(
        string imageUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                "media.invalid_image_url",
                "Image URL is required."));
        }

        if (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                "media.invalid_image_url",
                "Image URL must be an absolute http or https address."));
        }

        if (await IsBlockedHostAsync(uri.Host, cancellationToken))
        {
            return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                "media.image_url_blocked",
                "Image URL host is not allowed."));
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                    "media.image_fetch_failed",
                    $"Failed to download image (HTTP {(int)response.StatusCode})."));
            }

            if (response.Content.Headers.ContentLength is > MaxImageBytes)
            {
                return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                    "media.file_size_exceeded",
                    "Image file size exceeds the allowed 5 MB limit."));
            }

            // Re-check final URI after redirects.
            if (response.RequestMessage?.RequestUri is { } finalUri &&
                await IsBlockedHostAsync(finalUri.Host, cancellationToken))
            {
                return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                    "media.image_url_blocked",
                    "Image URL host is not allowed."));
            }

            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var memory = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxImageBytes)
                {
                    await memory.DisposeAsync();
                    return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                        "media.file_size_exceeded",
                        "Image file size exceeds the allowed 5 MB limit."));
                }

                await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (memory.Length <= 0)
            {
                await memory.DisposeAsync();
                return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                    "media.image_fetch_failed",
                    "Downloaded image was empty."));
            }

            memory.Position = 0;
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = ResolveFileName(uri, contentType);
            return ApplicationResult<MediaUploadFile>.Success(
                new MediaUploadFile(memory, fileName, contentType, memory.Length));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                "media.image_fetch_timeout",
                "Timed out while downloading the product image."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch external image candidate from {ImageUrl}", imageUrl);
            return ApplicationResult<MediaUploadFile>.Failure(new ApplicationError(
                "media.image_fetch_failed",
                "Failed to download the product image."));
        }
    }

    private static string ResolveFileName(Uri uri, string contentType)
    {
        var last = Path.GetFileName(uri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(last) && last.Contains('.', StringComparison.Ordinal))
        {
            return last;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/png" => "external-product.png",
            "image/webp" => "external-product.webp",
            _ => "external-product.jpg",
        };
    }

    private static async Task<bool> IsBlockedHostAsync(string host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(host, out var literal))
        {
            return IsPrivateOrSpecial(literal);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            return addresses.Length == 0 || addresses.Any(IsPrivateOrSpecial);
        }
        catch
        {
            return true;
        }
    }

    private static bool IsPrivateOrSpecial(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                   || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                   || (bytes[0] == 192 && bytes[1] == 168)
                   || (bytes[0] == 169 && bytes[1] == 254)
                   || bytes[0] == 127
                   || bytes[0] == 0;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                   || address.IsIPv6SiteLocal
                   || address.IsIPv6UniqueLocal
                   || address.IsIPv6Teredo;
        }

        return true;
    }
}
