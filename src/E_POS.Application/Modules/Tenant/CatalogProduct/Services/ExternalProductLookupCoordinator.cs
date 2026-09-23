using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Deterministic multi-provider coordinator for external product-data suggestions.
/// Zero configured/enabled providers → NO_MATCH. No catalogue or master-data writes.
/// </summary>
public sealed class ExternalProductLookupCoordinator : IExternalProductLookupCoordinator
{
    private readonly IReadOnlyList<IExternalProductLookupProvider> _providers;
    private readonly ISharedProductMetadataCacheRepository? _cacheRepository;
    private readonly ExternalProductLookupOptions _options;
    private readonly ILogger<ExternalProductLookupCoordinator> _logger;

    public ExternalProductLookupCoordinator(
        IEnumerable<IExternalProductLookupProvider> providers,
        IOptions<ExternalProductLookupOptions> options,
        ILogger<ExternalProductLookupCoordinator> logger,
        ISharedProductMetadataCacheRepository? cacheRepository = null)
    {
        _providers = providers?.ToArray() ?? Array.Empty<IExternalProductLookupProvider>();
        _options = options?.Value ?? new ExternalProductLookupOptions();
        _logger = logger;
        _cacheRepository = cacheRepository;
    }

    public async Task<ExternalProductLookupResult> LookupAsync(
        ExternalProductLookupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Identifier))
        {
            return new ExternalProductLookupResult(
                ExternalProductLookupStatuses.NoMatch,
                Suggestion: null,
                SourceReference: null,
                RetryAllowed: false);
        }

        var normalizedIdentifier = request.Identifier.Trim();

        // Preserve exact identifier string (including leading zeros) — do not parse as numeric.
        var ordered = ResolveEnabledProviders(request);
        if (ordered.Count == 0)
        {
            _logger.LogInformation(
                "External product lookup skipped: no enabled providers configured. Outcome={Outcome}",
                ExternalProductLookupStatuses.NoMatch);

            return new ExternalProductLookupResult(
                ExternalProductLookupStatuses.NoMatch,
                Suggestion: null,
                SourceReference: null,
                RetryAllowed: false);
        }

        var sawNoMatch = false;
        var sawTemporaryFailure = false;
        var cacheEnabled = _cacheRepository is not null && _options.Cache.Enabled;

        foreach (var (provider, timeoutSeconds) in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Check shared product metadata cache — scoped to THIS provider only. A cache hit
            // for one provider must never satisfy (or suppress) the lookup for another provider.
            if (cacheEnabled)
            {
                try
                {
                    var cached = await _cacheRepository!.GetValidAsync(normalizedIdentifier, provider.Name, cancellationToken)
                        .ConfigureAwait(false);

                    if (cached is not null)
                    {
                        _logger.LogInformation(
                            "Shared product metadata cache HIT for barcode {Barcode} Provider={Provider}. Outcome={Outcome}",
                            normalizedIdentifier,
                            cached.Provider,
                            ExternalProductLookupStatuses.Found);

                        return new ExternalProductLookupResult(
                            ExternalProductLookupStatuses.Found,
                            cached.Suggestion,
                            SourceReference: cached.Provider,
                            RetryAllowed: false,
                            SourceProvider: cached.Provider,
                            RetrievalSource: ExternalProductLookupRetrievalSources.Cache);
                    }

                    _logger.LogInformation(
                        "Shared product metadata cache MISS for barcode {Barcode} Provider={Provider}.",
                        normalizedIdentifier,
                        provider.Name);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Error reading from shared product metadata cache for barcode {Barcode} Provider={Provider}. Continuing to provider lookup.",
                        normalizedIdentifier,
                        provider.Name);
                }
            }

            // 2. Invoke provider
            var outcome = await InvokeProviderAsync(provider, request, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);

            if (outcome.Kind == ProviderInvokeKind.Found)
            {
                // Write normalized result to shared metadata cache
                if (cacheEnabled && outcome.Suggestion is not null)
                {
                    var ttl = TimeSpan.FromDays(_options.Cache.TtlDays > 0 ? _options.Cache.TtlDays : 30);
                    try
                    {
                        await _cacheRepository!.SetAsync(
                            normalizedIdentifier,
                            request.IdentifierStandard,
                            provider.Name,
                            outcome.Suggestion,
                            rawResponseJson: null,
                            ttl,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to write to cache for barcode {Barcode} Provider={Provider}. Lookup operation will continue unaffected.",
                            normalizedIdentifier,
                            provider.Name);
                    }
                }

                return new ExternalProductLookupResult(
                    ExternalProductLookupStatuses.Found,
                    outcome.Suggestion,
                    outcome.SourceReference,
                    RetryAllowed: false,
                    SourceProvider: provider.Name,
                    RetrievalSource: ExternalProductLookupRetrievalSources.Provider);
            }

            if (outcome.Kind == ProviderInvokeKind.NoMatch)
            {
                sawNoMatch = true;
            }
            else if (outcome.Kind == ProviderInvokeKind.TemporaryFailure)
            {
                sawTemporaryFailure = true;
            }
        }

        // Conservative mixed rule: any TEMPORARY_FAILURE without FOUND → TEMPORARY_FAILURE
        // (a failed provider might have known the product). Definitive NO_MATCH only when
        // every attempted provider completed with NO_MATCH.
        if (sawTemporaryFailure)
        {
            return new ExternalProductLookupResult(
                ExternalProductLookupStatuses.TemporaryFailure,
                Suggestion: null,
                SourceReference: null,
                RetryAllowed: true);
        }

        _ = sawNoMatch;
        return new ExternalProductLookupResult(
            ExternalProductLookupStatuses.NoMatch,
            Suggestion: null,
            SourceReference: null,
            RetryAllowed: false);
    }

    private IReadOnlyList<(IExternalProductLookupProvider Provider, int TimeoutSeconds)> ResolveEnabledProviders(
        ExternalProductLookupRequest request)
    {
        var configByName = (_options.Providers ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var defaultTimeout = _options.DefaultTimeoutSeconds > 0 ? _options.DefaultTimeoutSeconds : 5;

        return _providers
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p =>
            {
                if (!configByName.TryGetValue(p.Name.Trim(), out var cfg) || !cfg.Enabled)
                {
                    return ((IExternalProductLookupProvider Provider, int TimeoutSeconds, int Priority, string Name)?)null;
                }

                if (!p.CanHandle(request))
                {
                    return null;
                }

                var timeout = cfg.TimeoutSeconds is > 0 ? cfg.TimeoutSeconds.Value : defaultTimeout;
                return (p, timeout, cfg.Priority, p.Name.Trim());
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => (x.Provider, x.TimeoutSeconds))
            .ToArray();
    }

    private async Task<ProviderInvokeOutcome> InvokeProviderAsync(
        IExternalProductLookupProvider provider,
        ExternalProductLookupRequest request,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var result = await provider.LookupAsync(request, timeoutCts.Token).ConfigureAwait(false);
            result ??= new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure,
                Suggestion: null,
                ProviderReference: null,
                FailureCategory: "null_result");

            if (string.Equals(result.Status, ExternalProductLookupStatuses.Found, StringComparison.Ordinal))
            {
                if (ExternalProductSuggestionNormalizer.TryNormalizeFound(
                        request,
                        result,
                        out var suggestion,
                        out var sourceReference))
                {
                    _logger.LogInformation(
                        "External product lookup provider succeeded. Provider={Provider} Outcome={Outcome} IdentifierStandard={IdentifierStandard}",
                        provider.Name,
                        ExternalProductLookupStatuses.Found,
                        request.IdentifierStandard);

                    return ProviderInvokeOutcome.Found(suggestion, sourceReference);
                }

                _logger.LogInformation(
                    "External product lookup provider FOUND rejected by normalizer. Provider={Provider}",
                    provider.Name);

                return ProviderInvokeOutcome.NoMatch();
            }

            if (string.Equals(result.Status, ExternalProductLookupStatuses.NoMatch, StringComparison.Ordinal))
            {
                return ProviderInvokeOutcome.NoMatch();
            }

            return ProviderInvokeOutcome.TemporaryFailure();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "External product lookup provider timed out. Provider={Provider} TimeoutSeconds={TimeoutSeconds}",
                provider.Name,
                timeoutSeconds);
            return ProviderInvokeOutcome.TemporaryFailure();
        }
        catch (Exception ex)
        {
            // Contain provider failures — never leak exception details / secrets upward.
            _logger.LogWarning(
                ex,
                "External product lookup provider failed. Provider={Provider} FailureCategory={FailureCategory}",
                provider.Name,
                "provider_exception");
            return ProviderInvokeOutcome.TemporaryFailure();
        }
    }

    private enum ProviderInvokeKind
    {
        Found,
        NoMatch,
        TemporaryFailure,
    }

    private sealed record ProviderInvokeOutcome(
        ProviderInvokeKind Kind,
        ExternalProductSuggestion? Suggestion,
        string? SourceReference)
    {
        public static ProviderInvokeOutcome Found(ExternalProductSuggestion suggestion, string? sourceReference) =>
            new(ProviderInvokeKind.Found, suggestion, sourceReference);

        public static ProviderInvokeOutcome NoMatch() =>
            new(ProviderInvokeKind.NoMatch, null, null);

        public static ProviderInvokeOutcome TemporaryFailure() =>
            new(ProviderInvokeKind.TemporaryFailure, null, null);
    }
}
