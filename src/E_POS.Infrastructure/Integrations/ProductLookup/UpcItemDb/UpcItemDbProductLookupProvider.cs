using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Integrations.ProductLookup.UpcItemDb;

/// <summary>
/// Concrete external product lookup provider for UPCitemdb (https://www.upcitemdb.com).
/// Second provider in the priority chain, behind OpenFoodFacts. Translates between OneVerz
/// provider request/result and UPCitemdb's trial (free, keyless) lookup API. Keeps
/// provider-specific payloads and JSON encapsulated inside this adapter — see
/// <see cref="UpcItemDbDtos"/> for exactly which fields are modeled (never offers/pricing).
///
/// Category decision (documented per Phase C requirements): UPCitemdb's "category" field is a
/// free-text Google-product-taxonomy path with no stable per-category identifier — unlike
/// OpenFoodFacts' categories_hierarchy, which yields a deterministic "en:slug" key. Suggestions
/// from this provider therefore populate CategoryText only (display text) and leave
/// ExternalCategoryKey/ExternalCategoryName/ExternalCategoryHierarchy null, so no External
/// Category Mapping resolution is attempted for UPCitemdb-sourced products in this phase. This
/// intentionally avoids treating unrelated-provider text as if it were a stable mapping key.
/// </summary>
public sealed class UpcItemDbProductLookupProvider : IExternalProductLookupProvider
{
    public const string ProviderName = "upcitemdb";

    private const string TrialLookupPath = "/prod/trial/lookup";
    private const string PaidLookupPath = "/prod/v1/lookup";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ExternalProductLookupOptions _options;
    private readonly ILogger<UpcItemDbProductLookupProvider> _logger;
    private readonly Resilience.IProductLookupCircuitBreakerRegistry? _circuitBreakerRegistry;

    public UpcItemDbProductLookupProvider(
        HttpClient httpClient,
        IOptions<ExternalProductLookupOptions> options,
        ILogger<UpcItemDbProductLookupProvider> logger,
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

        // Conservative intersection of what OneVerz's ProductBarcodeFormatValidator already
        // classifies as a valid GTIN and what UPCitemdb's lookup endpoint documents support for
        // (UPC-A/EAN-8/EAN-13/GTIN-14) — mirrors OpenFoodFactsProductLookupProvider.CanHandle
        // exactly. ISBN is intentionally not special-cased here: any 13-digit ISBN already
        // passes as a GTIN-13 through the same digits-only/length check, so no separate handling
        // is required or safe to add in this phase.
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

        // 1. Circuit breaker gate — independent instance from OpenFoodFacts' breaker (registry is
        // keyed by provider name).
        var breaker = _circuitBreakerRegistry?.GetOrCreate(ProviderName);
        if (breaker is not null && _options.Resilience.CircuitBreaker.Enabled)
        {
            if (!breaker.TryExecute(out var rejectionReason))
            {
                _logger.LogWarning(
                    "UPCitemdb lookup rejected: circuit breaker is {CircuitState}. Reason={Reason}",
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

            // Status is TEMPORARY_FAILURE. Deliberately does NOT read/wait on the documented
            // X-RateLimit-Reset header — honoring it could require a long blocking wait inside a
            // user-facing HTTP request, which is unsafe here. A 429 is simply classified as a
            // retryable transient failure (bounded by the existing short backoff/max-retry
            // policy below) and, once retries are exhausted, the coordinator's existing fallback
            // rules take over exactly as they do for OpenFoodFacts.
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
                "UPCitemdb lookup attempt {Attempt} failed with {FailureCategory}. Retrying in {BackoffMs}ms...",
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

        // Same retry-category convention as OpenFoodFactsProductLookupProvider.
        return failureCategory is "rate_limited" or "timeout" or "network_error" or "http_408"
               || failureCategory.StartsWith("http_5", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ExternalProductLookupProviderResult> ExecuteSingleLookupAsync(
        ExternalProductLookupRequest request,
        CancellationToken cancellationToken)
    {
        var barcode = request.Identifier.Trim();
        var baseUrl = !string.IsNullOrWhiteSpace(_options.UpcItemDb.BaseUrl)
            ? _options.UpcItemDb.BaseUrl.TrimEnd('/')
            : UpcItemDbOptions.DefaultBaseUrl;

        var isPaidMode = string.Equals(_options.UpcItemDb.Mode, UpcItemDbOptions.PaidMode, StringComparison.OrdinalIgnoreCase);
        var lookupPath = isPaidMode ? PaidLookupPath : TrialLookupPath;

        var requestUrl = $"{baseUrl}{lookupPath}?upc={Uri.EscapeDataString(barcode)}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Paid-mode headers only — never populated from a hardcoded value. UserKey must come
        // from configuration/environment/secret store; trial mode sends neither header.
        if (isPaidMode && !string.IsNullOrWhiteSpace(_options.UpcItemDb.UserKey))
        {
            httpRequest.Headers.TryAddWithoutValidation("user_key", _options.UpcItemDb.UserKey);
            httpRequest.Headers.TryAddWithoutValidation("key_type", "3scale");
        }

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
                    "UPCitemdb lookup returned 404. Outcome={Outcome} DurationMs={DurationMs}",
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
                    "UPCitemdb lookup rate-limited (HTTP 429). Outcome={Outcome} DurationMs={DurationMs}",
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
                    "UPCitemdb lookup failed with HTTP status {StatusCode}. Outcome={Outcome} DurationMs={DurationMs}",
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

            UpcItemDbResponse? response;
            try
            {
                response = await JsonSerializer.DeserializeAsync<UpcItemDbResponse>(
                    contentStream,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "UPCitemdb returned malformed JSON. Outcome={Outcome}",
                    ExternalProductLookupStatuses.TemporaryFailure);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.TemporaryFailure,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: "malformed_json");
            }

            var matchedItem = SelectExactMatch(response?.Items, barcode);
            if (matchedItem is null)
            {
                _logger.LogInformation(
                    "UPCitemdb product not found or no exact barcode match among returned items " +
                    "(Code={Code}, Total={Total}). Outcome={Outcome} DurationMs={DurationMs}",
                    response?.Code,
                    response?.Total,
                    ExternalProductLookupStatuses.NoMatch,
                    elapsedMs);

                return new ExternalProductLookupProviderResult(
                    Status: ExternalProductLookupStatuses.NoMatch,
                    Suggestion: null,
                    ProviderReference: ProviderName,
                    FailureCategory: null);
            }

            var suggestion = MapItemToSuggestion(matchedItem, barcode, request.IdentifierStandard);

            _logger.LogInformation(
                "UPCitemdb product found. Outcome={Outcome} DurationMs={DurationMs}",
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
                "UPCitemdb lookup timed out. Outcome={Outcome}",
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
                "UPCitemdb network error. Outcome={Outcome}",
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
                "UPCitemdb unexpected provider failure. Outcome={Outcome}",
                ExternalProductLookupStatuses.TemporaryFailure);

            return new ExternalProductLookupProviderResult(
                Status: ExternalProductLookupStatuses.TemporaryFailure,
                Suggestion: null,
                ProviderReference: ProviderName,
                FailureCategory: "unexpected_error");
        }
    }

    /// <summary>
    /// Deterministic candidate selection (documented decision): UPCitemdb's items[] can contain
    /// multiple listings, including ones for a different barcode than requested. This provider
    /// only accepts items whose ean/upc/gtin exactly equals the requested (trimmed, leading
    /// zeros preserved) identifier — never a positional/first-item guess. When several items
    /// exactly match (duplicate listings for the same barcode), the first exact match in the
    /// response's own order is chosen, which is deterministic and reproducible; it is not
    /// resolved via any additional heuristic, keeping the rule simple and auditable.
    /// </summary>
    private static UpcItemDbItem? SelectExactMatch(List<UpcItemDbItem>? items, string requestedIdentifier)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        foreach (var item in items)
        {
            if (MatchesIdentifier(item.Ean, requestedIdentifier) ||
                MatchesIdentifier(item.Upc, requestedIdentifier) ||
                MatchesIdentifier(item.Gtin, requestedIdentifier))
            {
                return item;
            }
        }

        return null;
    }

    private static bool MatchesIdentifier(string? candidate, string requestedIdentifier) =>
        !string.IsNullOrWhiteSpace(candidate) &&
        string.Equals(candidate.Trim(), requestedIdentifier, StringComparison.Ordinal);

    private static ExternalProductSuggestion MapItemToSuggestion(
        UpcItemDbItem item,
        string barcode,
        string? identifierStandard)
    {
        var title = !string.IsNullOrWhiteSpace(item.Title) ? item.Title.Trim() : null;
        var description = !string.IsNullOrWhiteSpace(item.Description) ? item.Description.Trim() : null;
        var brandText = !string.IsNullOrWhiteSpace(item.Brand) ? item.Brand.Trim() : null;
        var categoryText = !string.IsNullOrWhiteSpace(item.Category) ? item.Category.Trim() : null;

        // "size" is the closest UPCitemdb analog to OpenFoodFacts' "quantity" -> UnitText.
        // model/color/dimension/weight have no canonical ExternalProductSuggestion field and are
        // intentionally NOT mapped (do not invent Product master fields).
        var unitText = !string.IsNullOrWhiteSpace(item.Size) ? item.Size.Trim() : null;

        var imageCandidate = item.Images?.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i))?.Trim();

        return new ExternalProductSuggestion(
            ProductName: title,
            ShortName: null,
            BrandText: brandText,
            CategoryText: categoryText,
            UnitText: unitText,
            CountryCode: null,
            ShortDescription: description,
            LongDescription: description,
            ImageCandidate: imageCandidate,
            PrimaryGtin: barcode,
            IdentifierStandard: identifierStandard,
            // Deliberately null: UPCitemdb has no stable per-category key (see class doc).
            ExternalCategoryKey: null,
            ExternalCategoryName: null,
            ExternalCategoryHierarchy: null);
    }
}
