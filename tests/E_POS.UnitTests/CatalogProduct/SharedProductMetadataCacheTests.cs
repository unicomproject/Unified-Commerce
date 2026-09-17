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

        var result = await repository.GetValidAsync(Barcode, CancellationToken.None);

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

        var cached = await repository.GetValidAsync(Barcode, CancellationToken.None);

        Assert.NotNull(cached);
        Assert.Equal("Diet Coke", cached!.ProductName);
        Assert.Equal("Coca-Cola", cached.BrandText);
        Assert.Equal(Barcode, cached.PrimaryGtin);

        var dbEntry = await dbContext.SharedProductMetadataCaches.SingleAsync();
        Assert.Equal(Barcode, dbEntry.NormalizedBarcode);
        Assert.Equal("openfoodfacts", dbEntry.Provider);
        Assert.Equal("EAN13", dbEntry.IdentifierStandard);
        Assert.Contains("Diet Coke", dbEntry.NormalizedMetadataJson);
        Assert.Contains("raw", dbEntry.RawResponseJson);
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

        var cached = await repository.GetValidAsync(Barcode, CancellationToken.None);

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

        var cached = await repository.GetValidAsync(Barcode, CancellationToken.None);
        Assert.NotNull(cached);
        Assert.Equal("Diet Coke v2", cached!.ProductName);
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
        Assert.Equal("cache", outcome.SourceReference);
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
        Assert.Equal(1, fakeProvider.CallCount);

        // 2. Second request -> Cache HIT -> returns cached result -> provider NOT called again
        var outcome2 = await coordinator.LookupAsync(request, CancellationToken.None);
        Assert.Equal(ExternalProductLookupStatuses.Found, outcome2.Status);
        Assert.Equal("Walkers", outcome2.Suggestion!.BrandText);
        Assert.Equal("cache", outcome2.SourceReference);
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

    private static ExternalProductSuggestion CreateSuggestion(string name, string brand) =>
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
            IdentifierStandard: "EAN13");

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

        public Task<ExternalProductSuggestion?> GetValidAsync(string normalizedBarcode, CancellationToken cancellationToken)
        {
            if (_throwOnRead)
            {
                throw new InvalidOperationException("Simulated cache read error");
            }
            return Task.FromResult<ExternalProductSuggestion?>(null);
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
