using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Tenant.OnlineStoreSetup.Services;

public sealed class DnsOverHttpsDomainVerificationProvider : IDomainVerificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly DomainVerificationOptions _options;

    public DnsOverHttpsDomainVerificationProvider(
        HttpClient httpClient,
        IOptions<DomainVerificationOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<DomainVerificationProviderResult> VerifyTxtRecordAsync(
        string domainName,
        string expectedTokenHash,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return new(DomainVerificationProviderStatus.Unavailable, "provider_disabled");
        }

        try
        {
            var recordName = $"{_options.RecordNamePrefix.Trim().TrimEnd('.')}.{domainName}";
            var separator = _options.QueryEndpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var requestUri = $"{_options.QueryEndpoint}{separator}name={Uri.EscapeDataString(recordName)}&type=TXT";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/dns-json");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new(DomainVerificationProviderStatus.Failed, $"dns_http_{(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("Answer", out var answers) ||
                answers.ValueKind != JsonValueKind.Array)
            {
                return new(DomainVerificationProviderStatus.NotFound, "txt_record_missing");
            }

            foreach (var answer in answers.EnumerateArray())
            {
                if (!answer.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var token = NormalizeTxtValue(data.GetString());
                if (token is not null && FixedTimeHashEquals(token, expectedTokenHash))
                {
                    return new(DomainVerificationProviderStatus.Verified);
                }
            }

            return new(DomainVerificationProviderStatus.NotFound, "verification_token_not_found");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(DomainVerificationProviderStatus.Timeout, "dns_timeout");
        }
        catch (HttpRequestException)
        {
            return new(DomainVerificationProviderStatus.Unavailable, "dns_unavailable");
        }
        catch (JsonException)
        {
            return new(DomainVerificationProviderStatus.Failed, "dns_response_invalid");
        }
        catch (Exception)
        {
            return new(DomainVerificationProviderStatus.Failed, "dns_provider_failed");
        }
    }

    private static string? NormalizeTxtValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length >= 2 && normalized[0] == '"' && normalized[^1] == '"')
        {
            normalized = normalized[1..^1];
        }

        return normalized.Replace("\\\" \\\"", string.Empty, StringComparison.Ordinal)
            .Replace("\\\"", "\"", StringComparison.Ordinal);
    }

    private static bool FixedTimeHashEquals(string token, string expectedHash)
    {
        var actualHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            .ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(actualHash),
            Encoding.ASCII.GetBytes(expectedHash));
    }
}
