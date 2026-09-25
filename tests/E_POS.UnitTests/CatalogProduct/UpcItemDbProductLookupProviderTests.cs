using System.Net;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Integrations.ProductLookup.Resilience;
using E_POS.Infrastructure.Integrations.ProductLookup.UpcItemDb;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class UpcItemDbProductLookupProviderTests
{
    private const string ValidEan13 = "5449000000996";
    private const string ValidUpcA = "012345678905";
    private const string ValidEan8 = "12345670";
    private const string ValidGtin14 = "05449000000996";

    [Fact]
    public void Name_ReturnsUpcItemDb()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        Assert.Equal("upcitemdb", provider.Name);
    }

    [Theory]
    [InlineData("12345670", true)]        // 8 digits (EAN-8)
    [InlineData("012345678905", true)]    // 12 digits (UPC-A)
    [InlineData("5449000000996", true)]   // 13 digits (EAN-13)
    [InlineData("05449000000996", true)]  // 14 digits (GTIN-14)
    [InlineData("12345", false)]          // invalid length
    [InlineData("abcdefghijklm", false)]  // non-numeric
    [InlineData("", false)]               // empty
    [InlineData(null, false)]             // null
    public void CanHandle_ValidatesBarcodeCorrectly(string? barcode, bool expected)
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var request = barcode is not null ? new ExternalProductLookupRequest(barcode, "EAN13", null) : null!;

        Assert.Equal(expected, provider.CanHandle(request));
    }

    [Fact]
    public async Task LookupAsync_ProductFound_MapsFieldsCorrectly()
    {
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "offset": 0,
            "items": [
                {
                    "ean": "{{ValidEan13}}",
                    "upc": "",
                    "title": "Coca-Cola Original Taste 330ml",
                    "description": "Classic carbonated soft drink",
                    "brand": "Coca-Cola",
                    "category": "Food, Beverages, Carbonated Soft Drinks",
                    "size": "330 ml",
                    "model": "N/A",
                    "color": "Red",
                    "dimension": "6 x 2 x 2 in",
                    "weight": "0.75 lb",
                    "images": ["https://img.example/coke-1.jpg", "https://img.example/coke-2.jpg"]
                }
            ]
        }
        """;

        var handler = new StubHttpMessageHandler(req =>
        {
            Assert.Contains("/prod/trial/lookup", req.RequestUri!.AbsolutePath);
            Assert.Contains($"upc={ValidEan13}", req.RequestUri!.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
            };
        });

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("upcitemdb", result.ProviderReference);
        Assert.Null(result.FailureCategory);
        Assert.NotNull(result.Suggestion);

        var s = result.Suggestion!;
        Assert.Equal("Coca-Cola Original Taste 330ml", s.ProductName);
        Assert.Equal("Coca-Cola", s.BrandText);
        Assert.Equal("Food, Beverages, Carbonated Soft Drinks", s.CategoryText);
        Assert.Equal("330 ml", s.UnitText);
        Assert.Equal("Classic carbonated soft drink", s.ShortDescription);
        Assert.Equal("Classic carbonated soft drink", s.LongDescription);
        Assert.Equal("https://img.example/coke-1.jpg", s.ImageCandidate);
        Assert.Equal(ValidEan13, s.PrimaryGtin);
        Assert.Equal("EAN13", s.IdentifierStandard);

        // Category decision (Phase C, documented): no stable key/hierarchy for UPCitemdb.
        Assert.Null(s.ExternalCategoryKey);
        Assert.Null(s.ExternalCategoryName);
        Assert.Null(s.ExternalCategoryHierarchy);

        // model/color/dimension/weight have no canonical suggestion field and must not leak
        // anywhere discoverable via reflection (mirrors the BrandId/CategoryId absence checks
        // used elsewhere in this suite for OpenFoodFacts).
        Assert.Null(s.GetType().GetProperty("Model"));
        Assert.Null(s.GetType().GetProperty("Color"));
        Assert.Null(s.GetType().GetProperty("Dimension"));
        Assert.Null(s.GetType().GetProperty("Weight"));

        // Passes through the existing normalizer cleanly, same as OpenFoodFacts suggestions.
        var normalized = ExternalProductSuggestionNormalizer.TryNormalizeFound(request, result, out var normalizedSuggestion, out var sourceRef);
        Assert.True(normalized);
        Assert.NotNull(normalizedSuggestion);
        Assert.Equal("Coca-Cola Original Taste 330ml", normalizedSuggestion!.ProductName);
        Assert.Equal("upcitemdb", sourceRef);
    }

    [Fact]
    public async Task LookupAsync_NoItems_ReturnsNoMatch()
    {
        var jsonResponse = """{ "code": "OK", "total": 0, "offset": 0, "items": [] }""";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("upcitemdb", result.ProviderReference);
        Assert.Null(result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_ItemsPresentButNoneMatchRequestedBarcode_ReturnsNoMatch()
    {
        // UPCitemdb can return near-miss/related items — none of which match the exact
        // requested identifier must never be silently accepted.
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "items": [
                { "ean": "9999999999999", "title": "Unrelated Product", "brand": "Other" }
            ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
    }

    [Fact]
    public async Task LookupAsync_MultipleItems_SelectsFirstExactBarcodeMatch()
    {
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 3,
            "items": [
                { "ean": "1111111111111", "title": "Wrong Item A", "brand": "A" },
                { "upc": "{{ValidEan13}}", "title": "Correct Item", "brand": "Correct Brand" },
                { "gtin": "{{ValidEan13}}", "title": "Duplicate Listing", "brand": "Duplicate Brand" }
            ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        // Deterministic rule: first exact match in response order wins ("Correct Item" via upc,
        // not the later duplicate "Duplicate Listing" via gtin).
        Assert.Equal("Correct Item", result.Suggestion!.ProductName);
        Assert.Equal("Correct Brand", result.Suggestion.BrandText);
    }

    [Fact]
    public async Task LookupAsync_MissingTitle_ProviderStillReturnsFound_NormalizerRejectsDownstream()
    {
        // The provider itself does not reject a missing title — that filtering is the shared
        // normalizer's responsibility (consistent with OpenFoodFactsProductLookupProvider).
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "items": [ { "ean": "{{ValidEan13}}", "brand": "NoTitleBrand" } ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");
        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Null(result.Suggestion!.ProductName);

        var normalized = ExternalProductSuggestionNormalizer.TryNormalizeFound(request, result, out var suggestion, out _);
        Assert.False(normalized);
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task LookupAsync_BrandAbsent_MapsNullBrandText()
    {
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "items": [ { "ean": "{{ValidEan13}}", "title": "No Brand Item" } ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Null(result.Suggestion!.BrandText);
    }

    [Fact]
    public async Task LookupAsync_CategoryAbsent_MapsNullCategoryText()
    {
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "items": [ { "ean": "{{ValidEan13}}", "title": "No Category Item", "brand": "B" } ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Null(result.Suggestion!.CategoryText);
    }

    [Fact]
    public async Task LookupAsync_NoImages_ImageCandidateIsNull()
    {
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "items": [ { "ean": "{{ValidEan13}}", "title": "No Image Item", "images": [] } ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Null(result.Suggestion!.ImageCandidate);
    }

    [Fact]
    public async Task LookupAsync_MarketplacePricingFieldsNeverLeak()
    {
        // Even if UPCitemdb's raw JSON contains offers/pricing (real API responses do), those
        // fields are never deserialized by UpcItemDbDtos, so they cannot possibly reach the
        // suggestion or any downstream serialization of the provider result.
        var jsonResponse = $$"""
        {
            "code": "OK",
            "total": 1,
            "items": [
                {
                    "ean": "{{ValidEan13}}",
                    "title": "Priced Item",
                    "brand": "B",
                    "lowest_recorded_price": 1.99,
                    "highest_recorded_price": 9.99,
                    "currency": "USD",
                    "offers": [
                        { "merchant": "SomeStore", "price": 4.99, "list_price": 6.99, "shipping": "Free", "availability": "In Stock", "link": "https://store.example/item" }
                    ]
                }
            ]
        }
        """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("offers", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("merchant", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("list_price", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("4.99", json, StringComparison.Ordinal);
        Assert.DoesNotContain("lowest_recorded_price", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SomeStore", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_MalformedJson_ReturnsTemporaryFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ not valid json at all ...", Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("malformed_json", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_Http429_ReturnsTemporaryFailureWithRateLimitedCategory()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage((HttpStatusCode)429));

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("rate_limited", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_Timeout_ReturnsTemporaryFailureWithTimeoutCategory()
    {
        // Simulates an internal timeout (an OperationCanceledException not tied to the caller's
        // own token) rather than the caller cancelling — hits the provider's timeout branch.
        var handler = new StubHttpMessageHandler(_ => throw new OperationCanceledException("simulated timeout"));

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("timeout", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_NetworkFailure_ReturnsTemporaryFailure()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));

        var provider = CreateProvider(handler);
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("network_error", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_Http500_ReturnsTemporaryFailure_Retried()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var provider = CreateProvider(handler, new ExternalProductLookupOptions
        {
            Resilience = new ProviderResilienceOptions { Enabled = true, MaxRetryAttempts = 2, RetryBackoffMilliseconds = 1 },
        });
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("http_500", result.FailureCategory);
        Assert.Equal(3, callCount); // initial attempt + 2 retries
    }

    [Fact]
    public async Task LookupAsync_NonRetryable4xx_ReturnsTemporaryFailure_NotRetried()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        var provider = CreateProvider(handler, new ExternalProductLookupOptions
        {
            Resilience = new ProviderResilienceOptions { Enabled = true, MaxRetryAttempts = 2, RetryBackoffMilliseconds = 1 },
        });
        var result = await provider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Equal("http_400", result.FailureCategory);
        Assert.Equal(1, callCount); // no retry for a non-retryable failure category
    }

    [Fact]
    public async Task LookupAsync_CancellationRequested_RethrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.LookupAsync(request, cts.Token));
    }

    [Fact]
    public async Task CircuitBreaker_UpcItemDbAndOpenFoodFacts_AreIndependent()
    {
        var timeProvider = new TestDateTimeProvider(DateTimeOffset.UtcNow);
        var options = Options.Create(new ExternalProductLookupOptions
        {
            Resilience = new ProviderResilienceOptions
            {
                Enabled = true,
                CircuitBreaker = new CircuitBreakerOptions { Enabled = true, FailureThreshold = 1, BreakDurationSeconds = 30 },
            },
        });
        var registry = new ProductLookupCircuitBreakerRegistry(timeProvider, options, NullLogger<ProductLookupCircuitBreaker>.Instance);

        var failingHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var upcProvider = new UpcItemDbProductLookupProvider(
            new HttpClient(failingHandler), options, NullLogger<UpcItemDbProductLookupProvider>.Instance, registry);

        // Trip the upcitemdb breaker (1 failure threshold).
        await upcProvider.LookupAsync(new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13"), CancellationToken.None);

        var upcBreaker = registry.GetOrCreate("upcitemdb");
        var offBreaker = registry.GetOrCreate("openfoodfacts");

        Assert.Equal(CircuitState.Open, upcBreaker.State);
        Assert.Equal(CircuitState.Closed, offBreaker.State); // untouched — independent instance
    }

    private static UpcItemDbProductLookupProvider CreateProvider(
        HttpMessageHandler handler,
        ExternalProductLookupOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        var opt = options ?? new ExternalProductLookupOptions();

        return new UpcItemDbProductLookupProvider(
            httpClient,
            Options.Create(opt),
            NullLogger<UpcItemDbProductLookupProvider>.Instance);
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public TestDateTimeProvider(DateTimeOffset initial) => UtcNow = initial;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            return Task.FromResult(_handler(request));
        }
    }
}
