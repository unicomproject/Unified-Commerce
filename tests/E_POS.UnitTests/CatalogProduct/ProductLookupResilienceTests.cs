using System.Net;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Integrations.ProductLookup.OpenFoodFacts;
using E_POS.Infrastructure.Integrations.ProductLookup.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductLookupResilienceTests
{
    private const string Barcode = "5449000000996";

    [Fact]
    public async Task Retry_Transient503_SucceedsOnSecondAttempt()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(req =>
        {
            attempts++;
            if (attempts == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidJsonResponse("Fanta"), Encoding.UTF8, "application/json"),
            };
        });

        var options = CreateOptions(maxRetries: 2, retryBackoffMs: 10);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("Fanta", result.Suggestion!.ProductName);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Retry_Transient503_ExhaustsRetries_ReturnsTemporaryFailure()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var options = CreateOptions(maxRetries: 2, retryBackoffMs: 10);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("http_503", result.FailureCategory);
        Assert.Equal(3, attempts); // Initial attempt + 2 retries = 3
    }

    [Fact]
    public async Task Retry_NonTransient404_DoesNotRetry()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var options = CreateOptions(maxRetries: 2, retryBackoffMs: 10);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Equal(1, attempts); // Did NOT retry
    }

    [Fact]
    public async Task Retry_NonTransient400_DoesNotRetry()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        var options = CreateOptions(maxRetries: 2, retryBackoffMs: 10);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("http_400", result.FailureCategory);
        Assert.Equal(1, attempts); // Did NOT retry
    }

    [Fact]
    public async Task Retry_MalformedJson_DoesNotRetry()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ broken json ...", Encoding.UTF8, "application/json"),
            };
        });

        var options = CreateOptions(maxRetries: 2, retryBackoffMs: 10);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("malformed_json", result.FailureCategory);
        Assert.Equal(1, attempts); // Did NOT retry
    }

    [Fact]
    public async Task Retry_RateLimited429_RetriesAndPreservesCategory()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage((HttpStatusCode)429);
        });

        var options = CreateOptions(maxRetries: 2, retryBackoffMs: 10);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("rate_limited", result.FailureCategory);
        Assert.Equal(3, attempts); // Initial + 2 retries
    }

    [Fact]
    public async Task CircuitBreaker_RepeatedFailures_OpensCircuitAndFailsFast()
    {
        var timeProvider = new TestDateTimeProvider(DateTimeOffset.UtcNow);
        var breaker = new ProductLookupCircuitBreaker(
            "openfoodfacts",
            failureThreshold: 2,
            breakDuration: TimeSpan.FromSeconds(30),
            timeProvider,
            NullLogger<ProductLookupCircuitBreaker>.Instance);

        var registry = new TestCircuitBreakerRegistry(breaker);

        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var options = CreateOptions(maxRetries: 0, breakerThreshold: 2, breakerDurationSeconds: 30);
        var provider = CreateProvider(handler, options, registry);
        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);

        // Failure 1
        var res1 = await provider.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, res1.Status);
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(1, attempts);

        // Failure 2 -> triggers circuit breaker open
        var res2 = await provider.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, res2.Status);
        Assert.Equal(CircuitState.Open, breaker.State);
        Assert.Equal(2, attempts);

        // Next call should FAIL FAST without touching HTTP
        var res3 = await provider.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, res3.Status);
        Assert.Equal("provider_unavailable", res3.FailureCategory);
        Assert.Equal(2, attempts); // Did NOT increment!
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpenSuccessfulProbe_ClosesCircuit()
    {
        var timeProvider = new TestDateTimeProvider(DateTimeOffset.UtcNow);
        var breaker = new ProductLookupCircuitBreaker(
            "openfoodfacts",
            failureThreshold: 1,
            breakDuration: TimeSpan.FromSeconds(30),
            timeProvider,
            NullLogger<ProductLookupCircuitBreaker>.Instance);

        var registry = new TestCircuitBreakerRegistry(breaker);

        var handler = new StubHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidJsonResponse("Sprite"), Encoding.UTF8, "application/json"),
            };
        });

        var options = CreateOptions(maxRetries: 0, breakerThreshold: 1, breakerDurationSeconds: 30);
        var provider = CreateProvider(handler, options, registry);
        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);

        // Force open
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        // Advance past break duration
        timeProvider.UtcNow = timeProvider.UtcNow.AddSeconds(31);

        // Probe request succeeds -> closes circuit
        var result = await provider.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpenFailedProbe_ReopensCircuit()
    {
        var timeProvider = new TestDateTimeProvider(DateTimeOffset.UtcNow);
        var breaker = new ProductLookupCircuitBreaker(
            "openfoodfacts",
            failureThreshold: 1,
            breakDuration: TimeSpan.FromSeconds(30),
            timeProvider,
            NullLogger<ProductLookupCircuitBreaker>.Instance);

        var registry = new TestCircuitBreakerRegistry(breaker);

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var options = CreateOptions(maxRetries: 0, breakerThreshold: 1, breakerDurationSeconds: 30);
        var provider = CreateProvider(handler, options, registry);
        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);

        // Force open
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        // Advance past break duration
        timeProvider.UtcNow = timeProvider.UtcNow.AddSeconds(31);

        // Probe request fails -> reopens circuit
        var result = await provider.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_OpenDoesNotAffectCacheHits()
    {
        var timeProvider = new TestDateTimeProvider(DateTimeOffset.UtcNow);
        var breaker = new ProductLookupCircuitBreaker(
            "openfoodfacts",
            failureThreshold: 1,
            breakDuration: TimeSpan.FromSeconds(30),
            timeProvider,
            NullLogger<ProductLookupCircuitBreaker>.Instance);

        // Force circuit open
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        var registry = new TestCircuitBreakerRegistry(breaker);

        var cachedSuggestion = new ExternalProductSuggestion(
            "Cached Cola", null, "Brand", null, null, null, null, null, null, Barcode, "EAN13");

        var stubCache = new StubCacheRepository(cachedSuggestion);

        var providerHandler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var options = CreateOptions(maxRetries: 0);
        var provider = CreateProvider(providerHandler, options, registry);

        var coordinator = new ExternalProductLookupCoordinator(
            [provider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            stubCache);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);

        // Cache HIT must return immediately even though circuit is OPEN
        var outcome = await coordinator.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, outcome.Status);
        Assert.Equal("Cached Cola", outcome.Suggestion!.ProductName);
        Assert.Equal("cache", outcome.SourceReference);
    }

    [Fact]
    public async Task CallerCancellation_PropagatesWithoutBeingConvertedToFailure()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var options = CreateOptions(maxRetries: 2);
        var provider = CreateProvider(handler, options);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", null);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.LookupAsync(request, cts.Token));
    }

    private static ExternalProductLookupOptions CreateOptions(
        int maxRetries = 2,
        int retryBackoffMs = 10,
        int breakerThreshold = 3,
        int breakerDurationSeconds = 30)
    {
        return new ExternalProductLookupOptions
        {
            DefaultTimeoutSeconds = 5,
            Providers =
            [
                new ExternalProductLookupProviderOptions
                {
                    Name = "openfoodfacts",
                    Enabled = true,
                    Priority = 1,
                    TimeoutSeconds = 4,
                },
            ],
            Resilience = new ProviderResilienceOptions
            {
                Enabled = true,
                MaxRetryAttempts = maxRetries,
                RetryBackoffMilliseconds = retryBackoffMs,
                CircuitBreaker = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureThreshold = breakerThreshold,
                    BreakDurationSeconds = breakerDurationSeconds,
                },
            },
        };
    }

    private static OpenFoodFactsProductLookupProvider CreateProvider(
        HttpMessageHandler handler,
        ExternalProductLookupOptions options,
        IProductLookupCircuitBreakerRegistry? registry = null)
    {
        return new OpenFoodFactsProductLookupProvider(
            new HttpClient(handler),
            Options.Create(options),
            NullLogger<OpenFoodFactsProductLookupProvider>.Instance,
            registry);
    }

    private static string ValidJsonResponse(string productName) => $$"""
    {
        "code": "{{Barcode}}",
        "status": 1,
        "product": {
            "product_name": "{{productName}}",
            "brands": "Brand"
        }
    }
    """;

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            return Task.FromResult(_handler(request));
        }
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public TestDateTimeProvider(DateTimeOffset initial) => UtcNow = initial;
    }

    private sealed class TestCircuitBreakerRegistry : IProductLookupCircuitBreakerRegistry
    {
        private readonly IProductLookupCircuitBreaker _breaker;
        public TestCircuitBreakerRegistry(IProductLookupCircuitBreaker breaker) => _breaker = breaker;
        public IProductLookupCircuitBreaker GetOrCreate(string providerName) => _breaker;
    }

    private sealed class StubCacheRepository : ISharedProductMetadataCacheRepository
    {
        private readonly ExternalProductSuggestion? _suggestion;
        public StubCacheRepository(ExternalProductSuggestion? suggestion) => _suggestion = suggestion;

        public Task<ExternalProductSuggestion?> GetValidAsync(string normalizedBarcode, CancellationToken cancellationToken) =>
            Task.FromResult(_suggestion);

        public Task SetAsync(string normalizedBarcode, string? identifierStandard, string provider, ExternalProductSuggestion suggestion, string? rawResponseJson, TimeSpan ttl, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
