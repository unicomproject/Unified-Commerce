using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Tenant.OnlineStoreSetup.Services;

public sealed class HttpCertificateProvisioningProvider : ICertificateProvisioningProvider
{
    private readonly HttpClient _httpClient;
    private readonly CertificateProvisioningOptions _options;

    public HttpCertificateProvisioningProvider(
        HttpClient httpClient,
        IOptions<CertificateProvisioningOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.BearerToken);
        }
    }

    public Task<CertificateProvisioningProviderResult> RequestAsync(
        Guid tenantId,
        Guid domainId,
        string domainName,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            _options.ProvisionEndpoint,
            tenantId,
            domainId,
            domainName,
            cancellationToken);

    public Task<CertificateProvisioningProviderResult> GetStatusAsync(
        Guid tenantId,
        Guid domainId,
        string domainName,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Get,
            _options.StatusEndpoint,
            tenantId,
            domainId,
            domainName,
            cancellationToken);

    private async Task<CertificateProvisioningProviderResult> SendAsync(
        HttpMethod method,
        string endpointTemplate,
        Guid tenantId,
        Guid domainId,
        string domainName,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return new(CertificateProvisioningProviderStatus.Unavailable, FailureCode: "provider_disabled");
        }

        try
        {
            var endpoint = endpointTemplate
                .Replace("{tenantId}", tenantId.ToString("D"), StringComparison.OrdinalIgnoreCase)
                .Replace("{domainId}", domainId.ToString("D"), StringComparison.OrdinalIgnoreCase)
                .Replace("{domainName}", Uri.EscapeDataString(domainName), StringComparison.OrdinalIgnoreCase);
            using var request = new HttpRequestMessage(method, endpoint);
            request.Headers.TryAddWithoutValidation("Idempotency-Key", $"online-store-domain-{domainId:N}");
            if (method == HttpMethod.Post)
            {
                request.Content = JsonContent.Create(new { tenantId, domainId, domainName });
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new(CertificateProvisioningProviderStatus.Failed, FailureCode: $"certificate_http_{(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<CertificateProviderPayload>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken);
            return payload is null
                ? new(CertificateProvisioningProviderStatus.Failed, FailureCode: "certificate_response_empty")
                : new(MapStatus(payload.Status), payload.IssuedAt, payload.ExpiresAt, payload.FailureCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(CertificateProvisioningProviderStatus.Timeout, FailureCode: "certificate_timeout");
        }
        catch (HttpRequestException)
        {
            return new(CertificateProvisioningProviderStatus.Unavailable, FailureCode: "certificate_unavailable");
        }
        catch (JsonException)
        {
            return new(CertificateProvisioningProviderStatus.Failed, FailureCode: "certificate_response_invalid");
        }
        catch (Exception)
        {
            return new(CertificateProvisioningProviderStatus.Failed, FailureCode: "certificate_provider_failed");
        }
    }

    private static CertificateProvisioningProviderStatus MapStatus(string? status) =>
        status?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => CertificateProvisioningProviderStatus.Active,
            "PROVISIONING" or "PENDING" => CertificateProvisioningProviderStatus.Provisioning,
            "NOT_REQUESTED" => CertificateProvisioningProviderStatus.NotRequested,
            "TIMEOUT" => CertificateProvisioningProviderStatus.Timeout,
            "UNAVAILABLE" => CertificateProvisioningProviderStatus.Unavailable,
            _ => CertificateProvisioningProviderStatus.Failed
        };

    private sealed record CertificateProviderPayload(
        string Status,
        DateTimeOffset? IssuedAt,
        DateTimeOffset? ExpiresAt,
        string? FailureCode);
}
