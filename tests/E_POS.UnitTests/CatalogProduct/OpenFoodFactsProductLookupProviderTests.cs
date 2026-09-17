using System.Net;
using System.Text;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Integrations.ProductLookup.OpenFoodFacts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class OpenFoodFactsProductLookupProviderTests
{
    private const string ValidEan13 = "5449000000996";
    private const string ValidUpcA = "012345678905";
    private const string ValidEan8 = "12345670";
    private const string ValidGtin14 = "05449000000996";

    [Fact]
    public void Name_ReturnsOpenFoodFacts()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        Assert.Equal("openfoodfacts", provider.Name);
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

        var canHandle = provider.CanHandle(request);

        Assert.Equal(expected, canHandle);
    }

    [Fact]
    public async Task LookupAsync_ProductFound_MapsFieldsCorrectly()
    {
        var jsonResponse = """
        {
            "code": "5449000000996",
            "status": 1,
            "status_verbose": "product found",
            "product": {
                "code": "5449000000996",
                "product_name": "Coca-Cola Original Taste",
                "generic_name": "Carbonated soft drink with cola flavor",
                "brands": "Coca-Cola",
                "categories": "Beverages, Carbonated drinks, Sodas, Colas",
                "quantity": "330 ml",
                "image_front_url": "https://images.openfoodfacts.org/images/products/544/900/000/0996/front_en.123.400.jpg",
                "countries_tags": ["en:united-kingdom", "en:france"]
            }
        }
        """;

        var handler = new StubHttpMessageHandler(req =>
        {
            Assert.Contains("/api/v2/product/5449000000996.json", req.RequestUri!.PathAndQuery);
            Assert.Contains("OneVerzEPOS", req.Headers.UserAgent.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
            };
        });

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("openfoodfacts", result.ProviderReference);
        Assert.Null(result.FailureCategory);
        Assert.NotNull(result.Suggestion);

        var s = result.Suggestion!;
        Assert.Equal("Coca-Cola Original Taste", s.ProductName);
        Assert.Equal("Carbonated soft drink with cola flavor", s.ShortName);
        Assert.Equal("Coca-Cola", s.BrandText);
        Assert.Equal("Beverages, Carbonated drinks, Sodas, Colas", s.CategoryText);
        Assert.Equal("330 ml", s.UnitText);
        Assert.Equal("https://images.openfoodfacts.org/images/products/544/900/000/0996/front_en.123.400.jpg", s.ImageCandidate);
        Assert.Equal(ValidEan13, s.PrimaryGtin);
        Assert.Equal("EAN13", s.IdentifierStandard);
        Assert.Equal("UNITED-K", s.CountryCode); // Truncated to 8 chars and uppercase

        // Ensure it passes through the existing normalizer cleanly
        var normalized = ExternalProductSuggestionNormalizer.TryNormalizeFound(request, result, out var normalizedSuggestion, out var sourceRef);
        Assert.True(normalized);
        Assert.NotNull(normalizedSuggestion);
        Assert.Equal("Coca-Cola Original Taste", normalizedSuggestion!.ProductName);
        Assert.Equal("openfoodfacts", sourceRef);
    }

    [Fact]
    public async Task LookupAsync_ProductNotFound_StatusZero_ReturnsNoMatch()
    {
        var jsonResponse = """
        {
            "code": "5449000000996",
            "status": 0,
            "status_verbose": "product not found"
        }
        """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("openfoodfacts", result.ProviderReference);
        Assert.Null(result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_Http404_ReturnsNoMatch()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("openfoodfacts", result.ProviderReference);
    }

    [Fact]
    public async Task LookupAsync_Http429_ReturnsTemporaryFailureWithRateLimitedCategory()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage((HttpStatusCode)429));

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("rate_limited", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_Http500_ReturnsTemporaryFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("http_500", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_MalformedJson_ReturnsTemporaryFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ not valid json at all ...", Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("malformed_json", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_NetworkFailure_ReturnsTemporaryFailure()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));

        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var result = await provider.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.Null(result.Suggestion);
        Assert.Equal("network_error", result.FailureCategory);
    }

    [Fact]
    public async Task LookupAsync_CancellationRequested_RethrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = CreateProvider(handler);
        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.LookupAsync(request, cts.Token));
    }

    [Fact]
    public async Task Coordinator_WithOpenFoodFactsProvider_SuccessfullyResolvesFoundProduct()
    {
        var jsonResponse = """
        {
            "code": "5449000000996",
            "status": 1,
            "product": {
                "product_name": "Sparkling Water",
                "brands": "Perrier",
                "quantity": "750ml",
                "image_front_url": "https://images.openfoodfacts.org/perrier.jpg"
            }
        }
        """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        });

        var options = new ExternalProductLookupOptions
        {
            DefaultTimeoutSeconds = 5,
            Providers =
            [
                new ExternalProductLookupProviderOptions
                {
                    Name = "openfoodfacts",
                    Enabled = true,
                    Priority = 1,
                },
            ],
            OpenFoodFacts = new OpenFoodFactsOptions
            {
                BaseUrl = "https://world.openfoodfacts.org",
            },
        };

        var provider = new OpenFoodFactsProductLookupProvider(
            new HttpClient(handler),
            Options.Create(options),
            NullLogger<OpenFoodFactsProductLookupProvider>.Instance);

        var coordinator = new ExternalProductLookupCoordinator(
            [provider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance);

        var request = new ExternalProductLookupRequest(ValidEan13, "EAN13", "EAN_13");

        var outcome = await coordinator.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, outcome.Status);
        Assert.NotNull(outcome.Suggestion);
        Assert.Equal("Sparkling Water", outcome.Suggestion!.ProductName);
        Assert.Equal("Perrier", outcome.Suggestion.BrandText);
        Assert.Equal("750ml", outcome.Suggestion.UnitText);
        Assert.Equal("https://images.openfoodfacts.org/perrier.jpg", outcome.Suggestion.ImageCandidate);
        Assert.Equal("openfoodfacts", outcome.SourceReference);
        Assert.False(outcome.RetryAllowed);
    }

    private static OpenFoodFactsProductLookupProvider CreateProvider(
        HttpMessageHandler handler,
        ExternalProductLookupOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        var opt = options ?? new ExternalProductLookupOptions();

        return new OpenFoodFactsProductLookupProvider(
            httpClient,
            Options.Create(opt),
            NullLogger<OpenFoodFactsProductLookupProvider>.Instance);
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
