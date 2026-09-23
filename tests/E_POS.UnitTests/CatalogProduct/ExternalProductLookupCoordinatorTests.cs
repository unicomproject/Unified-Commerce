using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalProductLookupCoordinatorTests
{
    private const string Identifier = "04006381333931";

    [Fact]
    public async Task LookupAsync_ZeroConfiguredProviders_ReturnsNoMatch()
    {
        var coordinator = CreateCoordinator(providers: [], options: new ExternalProductLookupOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
        Assert.False(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_DisabledProvider_NotInvoked_ReturnsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Cola"),
        };
        var options = OptionsWith(("alpha", false, 1));
        var coordinator = CreateCoordinator([provider], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OneProviderFound_ReturnsFound()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("House Lemon Juice", brand: "House", category: "Drinks", unit: "500ml"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.False(result.RetryAllowed);
        Assert.Equal("House Lemon Juice", result.Suggestion!.ProductName);
        Assert.Equal("House", result.Suggestion.BrandText);
        Assert.Equal("Drinks", result.Suggestion.CategoryText);
        Assert.Equal("500ml", result.Suggestion.UnitText);
        Assert.Equal(Identifier, result.Suggestion.PrimaryGtin);
        Assert.Equal("ref-1", result.SourceReference);
        Assert.Null(result.Suggestion.GetType().GetProperty("BrandId"));
        Assert.Null(result.Suggestion.GetType().GetProperty("CategoryId"));
    }

    [Fact]
    public async Task LookupAsync_OneProviderNoMatch_ReturnsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.False(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_OneProviderTemporaryFailure_ReturnsTemporaryFailure()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "timeout"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_ProviderThrows_MapsToTemporaryFailure()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Exception = new InvalidOperationException("boom"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_Timeout_ReturnsTemporaryFailure()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Delay = TimeSpan.FromSeconds(2),
            Result = FoundResult("Slow"),
        };
        var options = new ExternalProductLookupOptions
        {
            DefaultTimeoutSeconds = 1,
            Providers =
            [
                new ExternalProductLookupProviderOptions
                {
                    Name = "alpha",
                    Enabled = true,
                    Priority = 1,
                    TimeoutSeconds = 1,
                },
            ],
        };
        var coordinator = CreateCoordinator([provider], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_Cancellation_Propagates()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Delay = TimeSpan.FromSeconds(5),
            Result = FoundResult("X"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            coordinator.LookupAsync(CreateRequest(), cts.Token));
    }

    [Fact]
    public async Task LookupAsync_PreservesLeadingZeroIdentifier()
    {
        string? seen = null;
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            OnLookup = req =>
            {
                seen = req.Identifier;
                return FoundResult("Cola", primaryGtin: req.Identifier);
            },
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(
            new ExternalProductLookupRequest(Identifier, "GTIN14", "UNKNOWN"),
            CancellationToken.None);

        Assert.Equal(Identifier, seen);
        Assert.Equal(Identifier, result.Suggestion!.PrimaryGtin);
    }

    [Fact]
    public async Task LookupAsync_MalformedFound_WithoutProductName_TreatedAsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.Found,
                new ExternalProductSuggestion(null, null, null, null, null, null, null, null, null, Identifier, "GTIN13"),
                "ref",
                null),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
    }

    [Fact]
    public async Task LookupAsync_MismatchedIdentifier_RejectedAsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Wrong", primaryGtin: "9999999999999"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
    }

    [Fact]
    public async Task LookupAsync_ImageCandidate_RemainsUrlOnly()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Cola", imageCandidate: "https://cdn.example/p.png"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("https://cdn.example/p.png", result.Suggestion!.ImageCandidate);
    }

    [Fact]
    public async Task LookupAsync_InvalidImageCandidate_Omitted()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Cola", imageCandidate: "javascript:alert(1)"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Null(result.Suggestion!.ImageCandidate);
    }

    [Fact]
    public async Task LookupAsync_DoesNotReturnProviderCredentials()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.Found,
                new ExternalProductSuggestion(
                    "Cola", null, "Brand", null, null, null, null, null,
                    "https://cdn.example/p.png", Identifier, "GTIN13"),
                ProviderReference: "opaque-ref",
                FailureCategory: null),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("opaque-ref", result.SourceReference);
    }

    [Fact]
    public async Task LookupAsync_TruncatesProductNameToCanonicalLimit()
    {
        var longName = new string('A', ProductConstants.ProductNameMaxLength + 50);
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult(longName),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ProductConstants.ProductNameMaxLength, result.Suggestion!.ProductName!.Length);
    }

    [Fact]
    public async Task LookupAsync_PriorityOrder_Honored_StopsOnFirstFound()
    {
        var low = new FakeExternalProductLookupProvider("low")
        {
            Result = FoundResult("FromLow"),
        };
        var high = new FakeExternalProductLookupProvider("high")
        {
            Result = FoundResult("FromHigh"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "low", Enabled = true, Priority = 20 },
                new ExternalProductLookupProviderOptions { Name = "high", Enabled = true, Priority = 1 },
            ],
        };
        var coordinator = CreateCoordinator([low, high], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("FromHigh", result.Suggestion!.ProductName);
        Assert.Equal(1, high.CallCount);
        Assert.Equal(0, low.CallCount);
    }

    [Fact]
    public async Task LookupAsync_ANoMatch_BFound_ReturnsFound()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = FoundResult("FromB"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromB", result.Suggestion!.ProductName);
    }

    [Fact]
    public async Task LookupAsync_ATemporaryFailure_BFound_ReturnsFound()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "down"),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = FoundResult("FromB"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
    }

    [Fact]
    public async Task LookupAsync_AllNoMatch_ReturnsNoMatch()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
    }

    [Fact]
    public async Task LookupAsync_AllTemporaryFailure_ReturnsTemporaryFailure()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Exception = new TimeoutException(),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "5xx"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_MixedNoMatchAndTemporaryFailure_ReturnsTemporaryFailure()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "down"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    // --- Phase A: cache is now checked per-provider inside the priority loop, so it must never
    // let one provider's cached row satisfy (or suppress) another provider's lookup. ---

    [Fact]
    public async Task LookupAsync_CacheHitForHigherPriorityProvider_ShortCircuits_LowerPriorityProviderNeverCalled()
    {
        var cache = new InMemoryCacheRepository();
        cache.Seed(Identifier, "high", FoundResult("FromCacheHigh").Suggestion!);

        var low = new FakeExternalProductLookupProvider("low") { Result = FoundResult("FromLow") };
        var high = new FakeExternalProductLookupProvider("high") { Result = FoundResult("FromHigh") };
        var options = new ExternalProductLookupOptions
        {
            Cache = new ProductMetadataCacheOptions { Enabled = true, TtlDays = 30 },
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "high", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "low", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([high, low], options, cache);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromCacheHigh", result.Suggestion!.ProductName);
        Assert.Equal("high", result.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Cache, result.RetrievalSource);
        Assert.Equal(0, high.CallCount); // cache satisfied it — provider itself never invoked
        Assert.Equal(0, low.CallCount); // lower-priority provider never reached
    }

    [Fact]
    public async Task LookupAsync_CacheRowForOneProvider_DoesNotSatisfyDifferentProvidersIteration()
    {
        var cache = new InMemoryCacheRepository();
        cache.Seed(Identifier, "openfoodfacts", FoundResult("CachedForOff").Suggestion!);

        // Only "upcitemdb" is enabled — the cache row belongs to "openfoodfacts" and must be
        // invisible to upcitemdb's cache check, forcing a real provider call.
        var upcitemdb = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpcItemDb") };
        var options = new ExternalProductLookupOptions
        {
            Cache = new ProductMetadataCacheOptions { Enabled = true, TtlDays = 30 },
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "upcitemdb", Enabled = true, Priority = 1 },
            ],
        };
        var coordinator = CreateCoordinator([upcitemdb], options, cache);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromUpcItemDb", result.Suggestion!.ProductName);
        Assert.Equal("upcitemdb", result.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Provider, result.RetrievalSource);
        Assert.Equal(1, upcitemdb.CallCount); // real provider WAS called — cache did not falsely satisfy it
    }

    [Fact]
    public async Task LookupAsync_FreshFound_ReportsSourceProviderAndRetrievalSourceProvider()
    {
        var provider = new FakeExternalProductLookupProvider("alpha") { Result = FoundResult("Cola") };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("alpha", result.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Provider, result.RetrievalSource);
        Assert.NotEqual("cache", result.SourceProvider);
    }

    // --- Phase C: OpenFoodFacts (priority 1) -> UPCitemdb (priority 2) fallback scenarios.
    // These use FakeExternalProductLookupProvider named "openfoodfacts"/"upcitemdb" rather than
    // the real HTTP adapters — the adapters' own HTTP/JSON behavior is covered by
    // UpcItemDbProductLookupProviderTests.cs; this file verifies coordinator orchestration only. ---

    private static ExternalProductLookupOptions TwoProviderOptions(bool cacheEnabled = false, bool upcItemDbEnabled = true) => new()
    {
        Cache = new ProductMetadataCacheOptions { Enabled = cacheEnabled, TtlDays = 30 },
        Providers =
        [
            new ExternalProductLookupProviderOptions { Name = "openfoodfacts", Enabled = true, Priority = 1 },
            new ExternalProductLookupProviderOptions { Name = "upcitemdb", Enabled = upcItemDbEnabled, Priority = 2 },
        ],
    };

    // --- Config-hardening: upcitemdb.Enabled=false (the production-safe default) must fully
    // exclude it from the priority loop — never invoked, never contributing to the outcome —
    // while openfoodfacts keeps working normally at priority 1. ---

    [Fact]
    public async Task LookupAsync_UpcItemDbDisabled_OpenFoodFactsNoMatch_UpcItemDbNeverCalled_ReturnsNoMatch()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions(upcItemDbEnabled: false));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Equal(1, off.CallCount);
        Assert.Equal(0, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_UpcItemDbDisabled_OpenFoodFactsFound_UpcItemDbNeverCalled()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts") { Result = FoundResult("FromOff") };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions(upcItemDbEnabled: false));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("openfoodfacts", result.SourceProvider);
        Assert.Equal(1, off.CallCount);
        Assert.Equal(0, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_UpcItemDbEnabled_ParticipatesAsPriorityTwo_AfterOpenFoodFactsNoMatch()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions(upcItemDbEnabled: true));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("upcitemdb", result.SourceProvider);
        Assert.Equal(1, off.CallCount);
        Assert.Equal(1, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OpenFoodFactsFound_UpcItemDbNeverCalled()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts") { Result = FoundResult("FromOff") };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromOff", result.Suggestion!.ProductName);
        Assert.Equal("openfoodfacts", result.SourceProvider);
        Assert.Equal(1, off.CallCount);
        Assert.Equal(0, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OpenFoodFactsNoMatch_FallsThroughToUpcItemDbFound()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromUpc", result.Suggestion!.ProductName);
        Assert.Equal("upcitemdb", result.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Provider, result.RetrievalSource);
        Assert.Equal(1, off.CallCount);
        Assert.Equal(1, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OpenFoodFactsTemporaryFailure_FallsThroughToUpcItemDbFound()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.TemporaryFailure, null, null, "timeout"),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromUpc", result.Suggestion!.ProductName);
        Assert.Equal("upcitemdb", result.SourceProvider);
    }

    [Fact]
    public async Task LookupAsync_BothNoMatch_ReturnsOverallNoMatch()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.False(result.RetryAllowed);
        Assert.Equal(1, off.CallCount);
        Assert.Equal(1, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OpenFoodFactsTemporaryFailure_UpcItemDbNoMatch_ReturnsOverallTemporaryFailure()
    {
        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.TemporaryFailure, null, null, "down"),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        // Conservative rule (unchanged from Phase A/pre-existing coordinator logic): any
        // TEMPORARY_FAILURE without an eventual FOUND wins over a pure NO_MATCH.
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_OpenFoodFactsCacheHit_UpcItemDbNeverCalled()
    {
        var cache = new InMemoryCacheRepository();
        cache.Seed(Identifier, "openfoodfacts", FoundResult("CachedOff").Suggestion!);

        var off = new FakeExternalProductLookupProvider("openfoodfacts") { Result = FoundResult("FreshOff") };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FreshUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions(cacheEnabled: true), cache);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("CachedOff", result.Suggestion!.ProductName);
        Assert.Equal("openfoodfacts", result.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Cache, result.RetrievalSource);
        Assert.Equal(0, off.CallCount);
        Assert.Equal(0, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OpenFoodFactsNoMatch_UpcItemDbCacheHit_ReturnsUpcItemDbCachedResult()
    {
        var cache = new InMemoryCacheRepository();
        cache.Seed(Identifier, "upcitemdb", FoundResult("CachedUpc").Suggestion!);

        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FreshUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions(cacheEnabled: true), cache);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("CachedUpc", result.Suggestion!.ProductName);
        Assert.Equal("upcitemdb", result.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Cache, result.RetrievalSource);
        Assert.Equal(1, off.CallCount); // OFF's own cache missed, so OFF WAS invoked (and returned NoMatch)
        Assert.Equal(0, upc.CallCount); // UPC's cache hit meant the real UPC provider was never invoked
    }

    [Fact]
    public async Task LookupAsync_SameBarcodeCachedForBothProviders_PriorityDecidesWhichIsReturned()
    {
        var cache = new InMemoryCacheRepository();
        cache.Seed(Identifier, "openfoodfacts", FoundResult("CachedOff").Suggestion!);
        cache.Seed(Identifier, "upcitemdb", FoundResult("CachedUpc").Suggestion!);

        var off = new FakeExternalProductLookupProvider("openfoodfacts");
        var upc = new FakeExternalProductLookupProvider("upcitemdb");
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions(cacheEnabled: true), cache);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        // Priority 1 (openfoodfacts) is evaluated first in the loop, so its cache row wins —
        // the upcitemdb cache row is never even consulted.
        Assert.Equal("CachedOff", result.Suggestion!.ProductName);
        Assert.Equal("openfoodfacts", result.SourceProvider);
        Assert.Equal(0, off.CallCount);
        Assert.Equal(0, upc.CallCount);
    }

    [Fact]
    public async Task LookupAsync_TwoProvidersConfigured_OpenFoodFactsCategoryMappingContextUnaffected()
    {
        // Mandatory Phase C regression (per task §21): registering UPCitemdb as a second
        // provider must not change OpenFoodFacts' own category-key normalization/round-trip.
        var suggestionWithCategory = new ExternalProductSuggestion(
            "Coca Cola", ShortName: null, BrandText: "Coca-Cola", CategoryText: "Beverages, Colas",
            UnitText: null, CountryCode: null, ShortDescription: null, LongDescription: null,
            ImageCandidate: null, PrimaryGtin: Identifier, IdentifierStandard: "GTIN14",
            ExternalCategoryKey: "en:colas", ExternalCategoryName: "Colas",
            ExternalCategoryHierarchy: new[] { "en:beverages", "en:colas" });

        var off = new FakeExternalProductLookupProvider("openfoodfacts")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.Found, suggestionWithCategory, "openfoodfacts", null),
        };
        var upc = new FakeExternalProductLookupProvider("upcitemdb") { Result = FoundResult("FromUpc") };
        var coordinator = CreateCoordinator([off, upc], TwoProviderOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("openfoodfacts", result.SourceProvider);
        Assert.Equal("en:colas", result.Suggestion!.ExternalCategoryKey);
        Assert.Equal("Colas", result.Suggestion.ExternalCategoryName);
        Assert.Equal(2, result.Suggestion.ExternalCategoryHierarchy!.Count);
        Assert.Equal(0, upc.CallCount);
    }

    private sealed class InMemoryCacheRepository : ISharedProductMetadataCacheRepository
    {
        private readonly Dictionary<(string Barcode, string Provider), ExternalProductSuggestion> _entries = new();

        public void Seed(string barcode, string provider, ExternalProductSuggestion suggestion) =>
            _entries[(barcode, provider.ToLowerInvariant())] = suggestion;

        public Task<CachedExternalProductLookupResult?> GetValidAsync(string normalizedBarcode, string provider, CancellationToken cancellationToken)
        {
            var key = (normalizedBarcode, provider.ToLowerInvariant());
            return Task.FromResult(_entries.TryGetValue(key, out var suggestion)
                ? new CachedExternalProductLookupResult(suggestion, provider.ToLowerInvariant())
                : null);
        }

        public Task SetAsync(string normalizedBarcode, string? identifierStandard, string provider, ExternalProductSuggestion suggestion, string? rawResponseJson, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _entries[(normalizedBarcode, provider.ToLowerInvariant())] = suggestion;
            return Task.CompletedTask;
        }
    }

    private static ExternalProductLookupRequest CreateRequest() =>
        new(Identifier, "GTIN14", "UNKNOWN");

    private static ExternalProductLookupOptions OptionsWith(params (string Name, bool Enabled, int Priority)[] providers) =>
        new()
        {
            DefaultTimeoutSeconds = 5,
            Providers = providers
                .Select(p => new ExternalProductLookupProviderOptions
                {
                    Name = p.Name,
                    Enabled = p.Enabled,
                    Priority = p.Priority,
                })
                .ToList(),
        };

    private static ExternalProductLookupCoordinator CreateCoordinator(
        IEnumerable<IExternalProductLookupProvider> providers,
        ExternalProductLookupOptions options,
        ISharedProductMetadataCacheRepository? cacheRepository = null) =>
        new(
            providers,
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            cacheRepository);

    private static ExternalProductLookupProviderResult FoundResult(
        string productName,
        string? brand = null,
        string? category = null,
        string? unit = null,
        string? primaryGtin = Identifier,
        string? imageCandidate = null) =>
        new(
            ExternalProductLookupStatuses.Found,
            new ExternalProductSuggestion(
                productName,
                ShortName: null,
                BrandText: brand,
                CategoryText: category,
                UnitText: unit,
                CountryCode: null,
                ShortDescription: null,
                LongDescription: null,
                ImageCandidate: imageCandidate,
                PrimaryGtin: primaryGtin,
                IdentifierStandard: "GTIN13"),
            ProviderReference: "ref-1",
            FailureCategory: null);

    private sealed class FakeExternalProductLookupProvider : IExternalProductLookupProvider
    {
        public FakeExternalProductLookupProvider(string name) => Name = name;

        public string Name { get; }
        public int CallCount { get; private set; }
        public ExternalProductLookupProviderResult? Result { get; init; }
        public Exception? Exception { get; init; }
        public TimeSpan? Delay { get; init; }
        public Func<ExternalProductLookupRequest, ExternalProductLookupProviderResult>? OnLookup { get; init; }

        public bool CanHandle(ExternalProductLookupRequest request) => true;

        public async Task<ExternalProductLookupProviderResult> LookupAsync(
            ExternalProductLookupRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (Delay is not null)
            {
                await Task.Delay(Delay.Value, cancellationToken);
            }

            if (Exception is not null)
            {
                throw Exception;
            }

            if (OnLookup is not null)
            {
                return OnLookup(request);
            }

            return Result ?? new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.NoMatch, null, null, null);
        }
    }
}
