using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class SharedProductMetadataCacheTests
{
    private const string Barcode = "5449000000996";

    [Fact]
    public async Task GetValidAsync_CacheMiss_ReturnsNull()
    {
        var (repository, _) = CreateRepository();

        var result = await repository.GetValidAsync(Barcode, "openfoodfacts", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetValidAsync_ReturnsCachedSuggestion()
    {
        var (repository, dbContext) = CreateRepository();
        var suggestion = CreateSuggestion("Diet Coke", "Coca-Cola");

        await repository.SetAsync(
            Barcode,
            "EAN13",
            "openfoodfacts",
            suggestion,
            rawResponseJson: "{\"raw\": true}",
            ttl: TimeSpan.FromDays(30),
            CancellationToken.None);

        var cached = await repository.GetValidAsync(Barcode, "openfoodfacts", CancellationToken.None);

        Assert.NotNull(cached);
        Assert.Equal("openfoodfacts", cached!.Provider);
        Assert.Equal("Diet Coke", cached.Suggestion.ProductName);
        Assert.Equal("Coca-Cola", cached.Suggestion.BrandText);
        Assert.Equal(Barcode, cached.Suggestion.PrimaryGtin);

        var dbEntry = await dbContext.SharedProductMetadataCaches.SingleAsync();
        Assert.Equal(Barcode, dbEntry.NormalizedBarcode);
        Assert.Equal("openfoodfacts", dbEntry.Provider);
        Assert.Equal("EAN13", dbEntry.IdentifierStandard);
        Assert.Contains("Diet Coke", dbEntry.NormalizedMetadataJson);
        Assert.Contains("raw", dbEntry.RawResponseJson);
    }

    [Fact]
    public async Task SetAsync_ThenGetValidAsync_PreservesExternalCategoryMetadata_AndContainsNoTenantData()
    {
        var (repository, dbContext) = CreateRepository();
        var hierarchy = new[] { "en:beverages", "en:carbonated-drinks", "en:colas" };
        var suggestion = CreateSuggestion(
            "Coca Cola Zero",
            "Coca-Cola",
            externalCategoryKey: "en:colas",
            externalCategoryName: "Colas",
            externalCategoryHierarchy: hierarchy);

        await repository.SetAsync(
            Barcode,
            "EAN13",
            "openfoodfacts",
            suggestion,
            rawResponseJson: null,
            ttl: TimeSpan.FromDays(30),
            CancellationToken.None);

        var cached = await repository.GetValidAsync(Barcode, "openfoodfacts", CancellationToken.None);

        Assert.NotNull(cached);
        Assert.Equal("en:colas", cached!.Suggestion.ExternalCategoryKey);
        Assert.Equal("Colas", cached.Suggestion.ExternalCategoryName);
        Assert.NotNull(cached.Suggestion.ExternalCategoryHierarchy);
        Assert.Equal(3, cached.Suggestion.ExternalCategoryHierarchy!.Count);
        Assert.Equal("en:beverages", cached.Suggestion.ExternalCategoryHierarchy[0]);
        Assert.Equal("en:carbonated-drinks", cached.Suggestion.ExternalCategoryHierarchy[1]);
        Assert.Equal("en:colas", cached.Suggestion.ExternalCategoryHierarchy[2]);

        // Verify shared cache contains no tenant data
        var dbEntry = await dbContext.SharedProductMetadataCaches.SingleAsync();
        Assert.DoesNotContain("TenantId", dbEntry.NormalizedMetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TenantCategoryId", dbEntry.NormalizedMetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MappedCategoryId", dbEntry.NormalizedMetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MappingSource", dbEntry.NormalizedMetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetValidAsync_ExpiredEntry_ReturnsNull()
    {
        var timeProvider = new MutableDateTimeProvider(DateTimeOffset.UtcNow);
        var (repository, _) = CreateRepository(timeProvider);
        var suggestion = CreateSuggestion("Diet Coke", "Coca-Cola");

        await repository.SetAsync(
            Barcode,
            "EAN13",
            "openfoodfacts",
            suggestion,
            rawResponseJson: null,
            ttl: TimeSpan.FromDays(30),
            CancellationToken.None);

        // Fast forward 31 days into future
        timeProvider.UtcNow = timeProvider.UtcNow.AddDays(31);

        var cached = await repository.GetValidAsync(Barcode, "openfoodfacts", CancellationToken.None);

        Assert.Null(cached);
    }

    [Fact]
    public async Task SetAsync_ExistingRecord_PerformsUpsert()
    {
        var (repository, dbContext) = CreateRepository();
        var v1 = CreateSuggestion("Diet Coke v1", "Coca-Cola");
        var v2 = CreateSuggestion("Diet Coke v2", "Coca-Cola");

        await repository.SetAsync(Barcode, "EAN13", "openfoodfacts", v1, null, TimeSpan.FromDays(30), CancellationToken.None);
        await repository.SetAsync(Barcode, "EAN13", "openfoodfacts", v2, null, TimeSpan.FromDays(30), CancellationToken.None);

        var count = await dbContext.SharedProductMetadataCaches.CountAsync();
        Assert.Equal(1, count);

        var cached = await repository.GetValidAsync(Barcode, "openfoodfacts", CancellationToken.None);
        Assert.NotNull(cached);
        Assert.Equal("Diet Coke v2", cached!.Suggestion.ProductName);
    }

    [Fact]
    public async Task GetValidAsync_DifferentProvider_DoesNotReturnAnotherProvidersCacheRow()
    {
        var (repository, _) = CreateRepository();
        var suggestion = CreateSuggestion("Own Brand Cola", "House");

        await repository.SetAsync(Barcode, "EAN13", "openfoodfacts", suggestion, null, TimeSpan.FromDays(30), CancellationToken.None);

        // A row cached under "openfoodfacts" must be invisible to a lookup for "upcitemdb" —
        // one provider's cached data must never masquerade as another's.
        var cachedForOtherProvider = await repository.GetValidAsync(Barcode, "upcitemdb", CancellationToken.None);

        Assert.Null(cachedForOtherProvider);

        var cachedForRealProvider = await repository.GetValidAsync(Barcode, "openfoodfacts", CancellationToken.None);
        Assert.NotNull(cachedForRealProvider);
    }

    [Fact]
    public async Task Coordinator_CacheHit_ProviderNotCalled()
    {
        var (repository, _) = CreateRepository();
        var suggestion = CreateSuggestion("Mineral Water", "Nestle");

        await repository.SetAsync(Barcode, "EAN13", "openfoodfacts", suggestion, null, TimeSpan.FromDays(30), CancellationToken.None);

        var fakeProvider = new CountingProvider("openfoodfacts", CreateSuggestion("Other Product", "Other"));
        var options = CreateOptions(cacheEnabled: true);

        var coordinator = new ExternalProductLookupCoordinator(
            [fakeProvider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            repository);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", "EAN_13");

        var outcome = await coordinator.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, outcome.Status);
        Assert.Equal("Mineral Water", outcome.Suggestion!.ProductName);
        // Phase A fix: a cache hit must report the REAL provider, never the literal "cache".
        Assert.Equal("openfoodfacts", outcome.SourceReference);
        Assert.Equal("openfoodfacts", outcome.SourceProvider);
        Assert.NotEqual("cache", outcome.SourceReference);
        Assert.NotEqual("cache", outcome.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Cache, outcome.RetrievalSource);
        Assert.Equal(0, fakeProvider.CallCount); // Provider was never invoked!
    }

    [Fact]
    public async Task Coordinator_TwoSequentialRequests_FirstMissProviderCalled_SecondHitProviderZeroCalls()
    {
        var (repository, _) = CreateRepository();
        var fakeProvider = new CountingProvider("openfoodfacts", CreateSuggestion("Crisps", "Walkers"));
        var options = CreateOptions(cacheEnabled: true);

        var coordinator = new ExternalProductLookupCoordinator(
            [fakeProvider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            repository);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", "EAN_13");

        // 1. First request -> Cache MISS -> calls provider once -> writes to cache
        var outcome1 = await coordinator.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.Found, outcome1.Status);
        Assert.Equal("Walkers", outcome1.Suggestion!.BrandText);
        Assert.Equal("openfoodfacts", outcome1.SourceReference);
        Assert.Equal("openfoodfacts", outcome1.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Provider, outcome1.RetrievalSource);
        Assert.Equal(1, fakeProvider.CallCount);

        // 2. Second request -> Cache HIT -> returns cached result -> provider NOT called again.
        // Phase A fix: sourceProvider/sourceReference must still be "openfoodfacts" — the same
        // saved-mapping-relevant identity as the fresh lookup above — only retrievalSource differs.
        var outcome2 = await coordinator.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.Found, outcome2.Status);
        Assert.Equal("Walkers", outcome2.Suggestion!.BrandText);
        Assert.Equal("openfoodfacts", outcome2.SourceReference);
        Assert.Equal("openfoodfacts", outcome2.SourceProvider);
        Assert.Equal(outcome1.SourceProvider, outcome2.SourceProvider);
        Assert.Equal(ExternalProductLookupRetrievalSources.Cache, outcome2.RetrievalSource);
        Assert.Equal(1, fakeProvider.CallCount); // Still exactly 1 call!
    }

    [Fact]
    public async Task Coordinator_CacheReadThrows_GracefullyContinuesToProvider()
    {
        var throwingCache = new FailingCacheRepository(throwOnRead: true, throwOnWrite: false);
        var fakeProvider = new CountingProvider("openfoodfacts", CreateSuggestion("Tea", "Lipton"));
        var options = CreateOptions(cacheEnabled: true);

        var coordinator = new ExternalProductLookupCoordinator(
            [fakeProvider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            throwingCache);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", "EAN_13");

        var outcome = await coordinator.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, outcome.Status);
        Assert.Equal("Tea", outcome.Suggestion!.ProductName);
        Assert.Equal(1, fakeProvider.CallCount);
    }

    [Fact]
    public async Task Coordinator_CacheWriteThrows_DoesNotFailLookup()
    {
        var throwingCache = new FailingCacheRepository(throwOnRead: false, throwOnWrite: true);
        var fakeProvider = new CountingProvider("openfoodfacts", CreateSuggestion("Coffee", "Nescafe"));
        var options = CreateOptions(cacheEnabled: true);

        var coordinator = new ExternalProductLookupCoordinator(
            [fakeProvider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            throwingCache);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", "EAN_13");

        var outcome = await coordinator.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, outcome.Status);
        Assert.Equal("Coffee", outcome.Suggestion!.ProductName);
        Assert.Equal("openfoodfacts", outcome.SourceReference);
        Assert.Equal(1, fakeProvider.CallCount);
    }

    [Fact]
    public async Task Coordinator_ExpiredCache_FallsBackToProviderAndRefreshesCache()
    {
        var timeProvider = new MutableDateTimeProvider(DateTimeOffset.UtcNow);
        var (repository, _) = CreateRepository(timeProvider);

        var stale = CreateSuggestion("Old Granola", "Kellogg");
        await repository.SetAsync(Barcode, "EAN13", "openfoodfacts", stale, null, TimeSpan.FromDays(30), CancellationToken.None);

        // Fast forward 31 days
        timeProvider.UtcNow = timeProvider.UtcNow.AddDays(31);

        var fresh = CreateSuggestion("New Granola Recipe", "Kellogg");
        var fakeProvider = new CountingProvider("openfoodfacts", fresh);
        var options = CreateOptions(cacheEnabled: true);

        var coordinator = new ExternalProductLookupCoordinator(
            [fakeProvider],
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance,
            repository);

        var request = new ExternalProductLookupRequest(Barcode, "EAN13", "EAN_13");

        var outcome = await coordinator.LookupAsync(request, CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, outcome.Status);
        Assert.Equal("New Granola Recipe", outcome.Suggestion!.ProductName);
        Assert.Equal(1, fakeProvider.CallCount);
    }

    private static (SharedProductMetadataCacheRepository Repository, EPosDbContext DbContext) CreateRepository(
        IDateTimeProvider? dateTimeProvider = null)
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(databaseName: $"CacheDb_{Guid.NewGuid():N}")
            .Options;

        var dbContext = new EPosDbContext(options);
        var time = dateTimeProvider ?? new SystemDateTimeProvider();
        var repo = new SharedProductMetadataCacheRepository(
            dbContext,
            time,
            NullLogger<SharedProductMetadataCacheRepository>.Instance);

        return (repo, dbContext);
    }

    private static ExternalProductLookupOptions CreateOptions(bool cacheEnabled) =>
        new()
        {
            DefaultTimeoutSeconds = 5,
            Cache = new ProductMetadataCacheOptions
            {
                Enabled = cacheEnabled,
                TtlDays = 30,
            },
            Providers =
            [
                new ExternalProductLookupProviderOptions
                {
                    Name = "openfoodfacts",
                    Enabled = true,
                    Priority = 1,
                },
            ],
        };

    private static ExternalProductSuggestion CreateSuggestion(
        string name,
        string brand,
        string? externalCategoryKey = null,
        string? externalCategoryName = null,
        IReadOnlyList<string>? externalCategoryHierarchy = null) =>
        new(
            ProductName: name,
            ShortName: null,
            BrandText: brand,
            CategoryText: "Groceries",
            UnitText: "100g",
            CountryCode: "GB",
            ShortDescription: null,
            LongDescription: null,
            ImageCandidate: "https://example.com/img.jpg",
            PrimaryGtin: Barcode,
            IdentifierStandard: "EAN13",
            ExternalCategoryKey: externalCategoryKey,
            ExternalCategoryName: externalCategoryName,
            ExternalCategoryHierarchy: externalCategoryHierarchy);

    private sealed class MutableDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }

        public MutableDateTimeProvider(DateTimeOffset initial) => UtcNow = initial;
    }

    private sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class CountingProvider : IExternalProductLookupProvider
    {
        private readonly ExternalProductSuggestion _suggestion;

        public CountingProvider(string name, ExternalProductSuggestion suggestion)
        {
            Name = name;
            _suggestion = suggestion;
        }

        public string Name { get; }
        public int CallCount { get; private set; }

        public bool CanHandle(ExternalProductLookupRequest request) => true;

        public Task<ExternalProductLookupProviderResult> LookupAsync(ExternalProductLookupRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.Found,
                _suggestion,
                Name,
                FailureCategory: null));
        }
    }

    private sealed class FailingCacheRepository : ISharedProductMetadataCacheRepository
    {
        private readonly bool _throwOnRead;
        private readonly bool _throwOnWrite;

        public FailingCacheRepository(bool throwOnRead, bool throwOnWrite)
        {
            _throwOnRead = throwOnRead;
            _throwOnWrite = throwOnWrite;
        }

        public Task<CachedExternalProductLookupResult?> GetValidAsync(string normalizedBarcode, string provider, CancellationToken cancellationToken)
        {
            if (_throwOnRead)
            {
                throw new InvalidOperationException("Simulated cache read error");
            }
            return Task.FromResult<CachedExternalProductLookupResult?>(null);
        }

        public Task SetAsync(string normalizedBarcode, string? identifierStandard, string provider, ExternalProductSuggestion suggestion, string? rawResponseJson, TimeSpan ttl, CancellationToken cancellationToken)
        {
            if (_throwOnWrite)
            {
                throw new InvalidOperationException("Simulated cache write error");
            }
            return Task.CompletedTask;
        }
    }
}
