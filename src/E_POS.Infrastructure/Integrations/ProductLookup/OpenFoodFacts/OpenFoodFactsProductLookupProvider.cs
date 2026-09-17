using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Integrations.ProductLookup.OpenFoodFacts;

/// <summary>
/// Concrete external product lookup provider for Open Food Facts.
/// Translates between OneVerz provider request/result and Open Food Facts API v2.
/// Keeps provider-specific payloads and JSON encapsulated inside this adapter.
/// </summary>
public sealed class OpenFoodFactsProductLookupProvider : IExternalProductLookupProvider
{
    public const string ProviderName = "openfoodfacts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ExternalProductLookupOptions _options;
    private readonly ILogger<OpenFoodFactsProductLookupProvider> _logger;
    private readonly Resilience.IProductLookupCircuitBreakerRegistry? _circuitBreakerRegistry;

    public OpenFoodFactsProductLookupProvider(
        HttpClient httpClient,
        IOptions<ExternalProductLookupOptions> options,
        ILogger<OpenFoodFactsProductLookupProvider> logger,
        Resilience.IProductLookupCircuitBreakerRegistry? circuitBreakerRegistry = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? new ExternalProductLookupOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _circuitBreakerRegistry = circuitBreakerRegistry;
    }

    public string Name => ProviderName;

    public bool CanHandle(ExternalProductLookupRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Identifier))
        {
            return false;
        }

        var identifier = request.Identifier.Trim();

        // Open Food Facts indexes standard retail barcodes: EAN-8 (8), UPC-A (12), EAN-13 (13), and ITF-14/GTIN-14 (14).
        if (identifier.Length is not (8 or 12 or 13 or 14))
        {
            return false;
        }

        return identifier.All(char.IsAsciiDigit);
    }

    public async Task<ExternalProductLookupProviderResult> LookupAsync(
        ExternalProductLookupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Circuit breaker gate
        var breaker = _circuitBreakerRegistry?.GetOrCreate(ProviderName);
        if (breaker is not null && _options.Resilience.CircuitBreaker.Enabled)
        {
            if (!breaker.TryExecute(out var rejectionReason))
            {
                _logger.LogWarning(
                    "OpenFoodFacts lookup rejected: circuit breaker is {CircuitState}. Reason={Reason}",
                    breaker.State,
                    rejectionReason);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.TemporaryFailure,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: rejectionReason ?? "provider_unavailable");
            }
        }

        var maxRetries = _options.Resilience.Enabled
            ? Math.Max(0, _options.Resilience.MaxRetryAttempts)
            : 0;

        var backoffMs = _options.Resilience.RetryBackoffMilliseconds > 0
            ? _options.Resilience.RetryBackoffMilliseconds
            : 200;

        var attempt = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            var result = await ExecuteSingleLookupAsync(request, cancellationToken).ConfigureAwait(false);

            if (result.Status == ExternalProductLookupStatuses.Found)
            {
                breaker?.RecordSuccess();
                return result;
            }

            if (result.Status == ExternalProductLookupStatuses.NoMatch)
            {
                breaker?.RecordSuccess();
                return result;
            }

            // Status is TEMPORARY_FAILURE
            var isRetryable = IsRetryableFailure(result.FailureCategory);
            if (!isRetryable || attempt > maxRetries)
            {
                if (isRetryable)
                {
                    breaker?.RecordFailure();
                }

                return result;
            }

            _logger.LogInformation(
                "OpenFoodFacts lookup attempt {Attempt} failed with {FailureCategory}. Retrying in {BackoffMs}ms...",
                attempt,
                result.FailureCategory,
                backoffMs * attempt);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(backoffMs * attempt), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }
    }

    private static bool IsRetryableFailure(string? failureCategory)
    {
        if (string.IsNullOrWhiteSpace(failureCategory))
        {
            return false;
        }

        // Transient failures that should be retried:
        // rate_limited (429), timeout, network_error, http_5xx, http_408
        return failureCategory is "rate_limited" or "timeout" or "network_error" or "http_408"
               || failureCategory.StartsWith("http_5", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ExternalProductLookupProviderResult> ExecuteSingleLookupAsync(
        ExternalProductLookupRequest request,
        CancellationToken cancellationToken)
    {
        var barcode = request.Identifier.Trim();
        var baseUrl = !string.IsNullOrWhiteSpace(_options.OpenFoodFacts.BaseUrl)
            ? _options.OpenFoodFacts.BaseUrl.TrimEnd('/')
            : OpenFoodFactsOptions.DefaultBaseUrl;

        var requestUrl = $"{baseUrl}/api/v2/product/{Uri.EscapeDataString(barcode)}.json";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        var userAgent = !string.IsNullOrWhiteSpace(_options.OpenFoodFacts.UserAgent)
            ? _options.OpenFoodFacts.UserAgent
            : OpenFoodFactsOptions.DefaultUserAgent;

        if (ProductInfoHeaderValue.TryParse(userAgent, out var parsedUserAgent))
        {
            httpRequest.Headers.UserAgent.Add(parsedUserAgent);
        }
        else
        {
            httpRequest.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var startTime = DateTime.UtcNow;

        try
        {
            using var httpResponse = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "OpenFoodFacts lookup returned 404. Outcome={Outcome} DurationMs={DurationMs}",
                    ExternalProductLookupStatuses.NoMatch,
                    elapsedMs);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.NoMatch,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: null);
            }

            if (httpResponse.StatusCode == (HttpStatusCode)429)
            {
                _logger.LogWarning(
                    "OpenFoodFacts lookup rate-limited (HTTP 429). Outcome={Outcome} DurationMs={DurationMs}",
                    ExternalProductLookupStatuses.TemporaryFailure,
                    elapsedMs);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.TemporaryFailure,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: "rate_limited");
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenFoodFacts lookup failed with HTTP status {StatusCode}. Outcome={Outcome} DurationMs={DurationMs}",
                    (int)httpResponse.StatusCode,
                    ExternalProductLookupStatuses.TemporaryFailure,
                    elapsedMs);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.TemporaryFailure,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: $"http_{(int)httpResponse.StatusCode}");
            }

            using var contentStream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            OpenFoodFactsResponse? offResponse;
            try
            {
                offResponse = await JsonSerializer.DeserializeAsync<OpenFoodFactsResponse>(
                    contentStream,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "OpenFoodFacts returned malformed JSON. Outcome={Outcome}",
                    ExternalProductLookupStatuses.TemporaryFailure);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.TemporaryFailure,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: "malformed_json");
            }

            if (offResponse is null || offResponse.Status != 1 || offResponse.Product is null)
            {
                _logger.LogInformation(
                    "OpenFoodFacts product not found (Status={Status}). Outcome={Outcome} DurationMs={DurationMs}",
                    offResponse?.Status,
                    ExternalProductLookupStatuses.NoMatch,
                    elapsedMs);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.NoMatch,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: null);
            }

            var suggestion = MapProductToSuggestion(offResponse.Product, barcode, request.IdentifierStandard);

            _logger.LogInformation(
                "OpenFoodFacts product found. Outcome={Outcome} DurationMs={DurationMs}",
                ExternalProductLookupStatuses.Found,
                elapsedMs);

            return new ExternalProductLookupProviderResult(
                Status: ExternalProductLookupStatuses.Found,
                Suggestion: suggestion,
                ProviderReference: ProviderName,
                FailureCategory: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "OpenFoodFacts lookup timed out. Outcome={Outcome}",
                ExternalProductLookupStatuses.TemporaryFailure);

            return new ExternalProductLookupProviderResult(
                Status: ExternalProductLookupStatuses.TemporaryFailure,
                Suggestion: null,
                ProviderReference: ProviderName,
                FailureCategory: "timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "OpenFoodFacts network error. Outcome={Outcome}",
                ExternalProductLookupStatuses.TemporaryFailure);

            return new ExternalProductLookupProviderResult(
                Status: ExternalProductLookupStatuses.TemporaryFailure,
                Suggestion: null,
                ProviderReference: ProviderName,
                FailureCategory: "network_error");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "OpenFoodFacts unexpected provider failure. Outcome={Outcome}",
                ExternalProductLookupStatuses.TemporaryFailure);

            return new ExternalProductLookupProviderResult(
                Status: ExternalProductLookupStatuses.TemporaryFailure,
                Suggestion: null,
                ProviderReference: ProviderName,
                FailureCategory: "unexpected_error");
        }
    }

    private static ExternalProductSuggestion MapProductToSuggestion(
        OpenFoodFactsProduct product,
        string barcode,
        string? identifierStandard)
    {
        var productName = !string.IsNullOrWhiteSpace(product.ProductName)
            ? product.ProductName.Trim()
            : (!string.IsNullOrWhiteSpace(product.ProductNameEn) ? product.ProductNameEn.Trim() : null);

        var shortName = !string.IsNullOrWhiteSpace(product.GenericName)
            ? product.GenericName.Trim()
            : (!string.IsNullOrWhiteSpace(product.GenericNameEn) ? product.GenericNameEn.Trim() : null);

        var brandText = !string.IsNullOrWhiteSpace(product.Brands)
            ? product.Brands.Trim()
            : null;

        var categoryText = !string.IsNullOrWhiteSpace(product.Categories)
            ? product.Categories.Trim()
            : null;

        var unitText = !string.IsNullOrWhiteSpace(product.Quantity)
            ? product.Quantity.Trim()
            : null;

        var countryCode = ResolveCountryCode(product.CountriesTags);

        var imageCandidate = !string.IsNullOrWhiteSpace(product.ImageFrontUrl)
            ? product.ImageFrontUrl.Trim()
            : (!string.IsNullOrWhiteSpace(product.ImageUrl)
                ? product.ImageUrl.Trim()
                : (!string.IsNullOrWhiteSpace(product.ImageFrontSmallUrl) ? product.ImageFrontSmallUrl.Trim() : null));

        var shortDescription = shortName ?? productName;
        var longDescription = !string.IsNullOrWhiteSpace(product.GenericName)
            ? product.GenericName.Trim()
            : product.GenericNameEn?.Trim();

        return new ExternalProductSuggestion(
            ProductName: productName,
            ShortName: shortName,
            BrandText: brandText,
            CategoryText: categoryText,
            UnitText: unitText,
            CountryCode: countryCode,
            ShortDescription: shortDescription,
            LongDescription: longDescription,
            ImageCandidate: imageCandidate,
            PrimaryGtin: barcode,
            IdentifierStandard: identifierStandard);
    }

    private static string? ResolveCountryCode(List<string>? countriesTags)
    {
        if (countriesTags is null || countriesTags.Count == 0)
        {
            return null;
        }

        var firstTag = countriesTags.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        if (string.IsNullOrWhiteSpace(firstTag))
        {
            return null;
        }

        var cleaned = firstTag.Trim();
        if (cleaned.StartsWith("en:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..];
        }

        return cleaned.Length <= 8 ? cleaned.ToUpperInvariant() : cleaned[..8].ToUpperInvariant();
    }
}
