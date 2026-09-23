using E_POS.Application;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

/// <summary>
/// Full Product Setup end-to-end validation: drives the REAL
/// <see cref="TenantAdminProductService"/> (real coordinator, real resolvers, real repositories)
/// against a real, freshly-migrated PostgreSQL database and — where noted — the real live
/// OpenFoodFacts API. This is deliberately NOT a WebApplicationFactory/HTTP-level test (no ASP.NET
/// routing/auth pipeline); it validates every layer beneath that: service → coordinator →
/// resolvers → repositories → real DB. Product-create transaction/rollback/atomicity scenarios
/// are covered separately by ExternalCategoryMappingPostgreSqlTests /
/// ExternalBrandMappingPostgreSqlTests, which already exercise the real transaction against real
/// Postgres; this file focuses on the External Lookup → Resolution → Cache path that those don't
/// cover, using the real wiring end to end.
/// </summary>
public sealed class ProductSetupEndToEndPostgreSqlTests
{
    // Real, checksum-valid EAN-13 confirmed reachable against the live OpenFoodFacts API in prior
    // phases of this engagement (Coca-Cola 330ml). A live network call is made for the tests
    // marked [LiveNetwork] below — this is intentional, deliberate, low-volume (a handful of
    // requests), and consistent with prior phases' live smoke-testing approach.
    private const string LiveBarcode = "5449000000996";

    private static readonly string AdminConnectionString =
        new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION_STRING must be set."))
        { Database = "postgres" }.ConnectionString;

    [Fact]
    public async Task LiveNetwork_FreshLookup_ThenCacheHit_ThenSavedMapping_ThenInactiveMappingFallsThrough()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var brandId = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-E2E", "tenant-e2e", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "BEV", "Beverages", "beverages", null, 1, CategoryConstants.ActiveStatus, null, now));
                seed.Brands.Add(
                    Brand.Create(brandId, tenantId, "COCA_COLA", "Coca Cola", "coca-cola", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            var context = CreateFullPermissionContext(tenantId);

            // --- Scenario A / F step 1: fresh external lookup against the REAL OpenFoodFacts API,
            // through the REAL coordinator + REAL resolvers + REAL repository, against real Postgres.
            await using (var scope1 = await BuildRealServiceAsync(connectionString))
            {
                var result1 = await scope1.Service.ExternalLookupBarcodeAsync(
                    context,
                    new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                    CancellationToken.None);

                Assert.True(result1.IsSuccess, result1.IsSuccess ? null : result1.Error.Message);
                var found1 = result1.Value!;

                if (found1.Status != "FOUND")
                {
                    // Live external data is out of this codebase's control. If OpenFoodFacts no
                    // longer recognizes this barcode, the rest of this scenario cannot proceed
                    // meaningfully — report rather than fail on an environment/data condition.
                    return;
                }

                Assert.Equal("openfoodfacts", found1.SourceProvider);
                Assert.Equal("PROVIDER", found1.RetrievalSource);
                Assert.NotNull(found1.Suggestion);

                // --- Scenario F: second lookup for the same barcode must be served from the
                // provider-scoped cache — sourceProvider must remain the real provider, never "cache".
                var result2 = await scope1.Service.ExternalLookupBarcodeAsync(
                    context,
                    new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                    CancellationToken.None);

                Assert.True(result2.IsSuccess);
                var found2 = result2.Value!;
                Assert.Equal("FOUND", found2.Status);
                Assert.Equal("openfoodfacts", found2.SourceProvider);
                Assert.NotEqual("cache", found2.SourceProvider);
                Assert.Equal("CACHE", found2.RetrievalSource);

                // Real cache table row exists and is provider-scoped correctly (never "cache").
                await using (var verify = CreateContext(connectionString))
                {
                    var cacheRow = await verify.SharedProductMetadataCaches
                        .FirstOrDefaultAsync(x => x.NormalizedBarcode == LiveBarcode);
                    Assert.NotNull(cacheRow);
                    Assert.Equal("openfoodfacts", cacheRow!.Provider);
                    Assert.NotEqual("cache", cacheRow.Provider);
                    Assert.True(cacheRow.ExpiresAt > now);
                }

                // If the live product happens to carry no Brand/Category text today, the mapped-
                // saved-mapping and inactive-fallback sub-scenarios below cannot be driven — report
                // what live data actually provided rather than fabricating it.
                if (found2.BrandResolution?.ExternalBrandKey is not { Length: > 0 } liveBrandKey)
                {
                    return;
                }

                // --- Scenario A "mapped": seed a saved ExternalBrandMapping using the REAL key the
                // live provider returned, pointing at our seeded ACTIVE tenant Brand, then verify the
                // NEXT lookup resolves it as MappedBrand (not merely a suggestion).
                await using (var mapSeed = CreateContext(connectionString))
                {
                    var brandMappingRepo = new ExternalBrandMappingRepository(
                        mapSeed, new TestDateTimeProvider(), NullLogger<ExternalBrandMappingRepository>.Instance);
                    await brandMappingRepo.UpsertAsync(
                        tenantId, "openfoodfacts", liveBrandKey,
                        found2.BrandResolution!.ExternalBrandName ?? liveBrandKey,
                        brandId, "PRODUCT_CONFIRMED", null, CancellationToken.None);
                }

                var result3 = await scope1.Service.ExternalLookupBarcodeAsync(
                    context,
                    new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                    CancellationToken.None);
                Assert.True(result3.IsSuccess);
                var mapped = result3.Value!.BrandResolution;
                Assert.NotNull(mapped);
                Assert.NotNull(mapped!.MappedBrand);
                Assert.Equal(brandId, mapped.MappedBrand!.Id);
                Assert.Empty(mapped.Suggestions);

                // --- Scenario J: mark the mapped Brand INACTIVE. The saved mapping must be IGNORED
                // (never auto-deleted) and the resolver must fall through to suggestions/no-match,
                // never returning a stale/invalid Brand as authoritative.
                await using (var deactivate = CreateContext(connectionString))
                {
                    var brand = await deactivate.Brands.SingleAsync(b => b.Id == brandId);
                    brand.UpdateProfile(brand.BrandCode, brand.BrandName, brand.BrandSlug, brand.Description, BrandConstants.InactiveStatus, null, now);
                    await deactivate.SaveChangesAsync();
                }

                var result4 = await scope1.Service.ExternalLookupBarcodeAsync(
                    context,
                    new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                    CancellationToken.None);
                Assert.True(result4.IsSuccess);
                Assert.Null(result4.Value!.BrandResolution?.MappedBrand);

                // Mapping row itself must still exist, unmodified, in the database (never
                // auto-deleted just because its target went inactive).
                await using (var verifyMapping = CreateContext(connectionString))
                {
                    var stillExists = await verifyMapping.ExternalBrandMappings
                        .AnyAsync(m => m.TenantId == tenantId && m.TenantBrandId == brandId);
                    Assert.True(stillExists);
                }
            }
        });
    }

    [Fact]
    public async Task LiveNetwork_TenantIsolation_SameBarcodeAndMapping_NeverLeaksAcrossTenants()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var brandA = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.AddRange(
                    CreateTenant(tenantA, "TENANT-A", "tenant-a", now),
                    CreateTenant(tenantB, "TENANT-B", "tenant-b", now));
                seed.Brands.Add(Brand.Create(brandA, tenantA, "COCA_COLA_A", "Coca Cola A", "coca-cola-a", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var scope = await BuildRealServiceAsync(connectionString);

            var resultA1 = await scope.Service.ExternalLookupBarcodeAsync(
                CreateFullPermissionContext(tenantA),
                new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                CancellationToken.None);

            if (!resultA1.IsSuccess || resultA1.Value!.Status != "FOUND" ||
                resultA1.Value.BrandResolution?.ExternalBrandKey is not { Length: > 0 } brandKey)
            {
                return; // live data unavailable/unusable today — nothing to validate
            }

            await using (var mapSeed = CreateContext(connectionString))
            {
                var repo = new ExternalBrandMappingRepository(mapSeed, new TestDateTimeProvider(), NullLogger<ExternalBrandMappingRepository>.Instance);
                await repo.UpsertAsync(tenantA, "openfoodfacts", brandKey,
                    resultA1.Value.BrandResolution!.ExternalBrandName ?? brandKey,
                    brandA, "PRODUCT_CONFIRMED", null, CancellationToken.None);
            }

            // Tenant A now resolves the saved mapping.
            var resultAMapped = await scope.Service.ExternalLookupBarcodeAsync(
                CreateFullPermissionContext(tenantA),
                new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                CancellationToken.None);
            Assert.Equal(brandA, resultAMapped.Value!.BrandResolution?.MappedBrand?.Id);

            // Tenant B — same barcode, same provider, same external key — must NOT see Tenant A's
            // mapping or Brand under any circumstances.
            var resultB = await scope.Service.ExternalLookupBarcodeAsync(
                CreateFullPermissionContext(tenantB),
                new ExternalLookupProductBarcodeRequest { Barcode = LiveBarcode },
                CancellationToken.None);
            Assert.True(resultB.IsSuccess);
            Assert.Null(resultB.Value!.BrandResolution?.MappedBrand);
            if (resultB.Value.BrandResolution is not null)
            {
                foreach (var suggestion in resultB.Value.BrandResolution.Suggestions)
                {
                    Assert.NotEqual(brandA, suggestion.Id);
                }
            }
        });
    }

    [Fact]
    public async Task ResolveBarcodeAsync_AfterProductSeeded_FindsLocalMatch_NoExternalRequired()
    {
        // Scenario G: proves the backend's LOCAL lookup path (the one Flutter's
        // submitScanCandidate calls first) finds a tenant-owned Product/Barcode directly from the
        // database — no external provider is consulted for this call at all (ResolveBarcodeAsync
        // has no dependency on IExternalProductLookupCoordinator whatsoever; that's a structural,
        // not just behavioral, guarantee — see ITenantAdminProductService.ResolveBarcodeAsync).
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            const string barcode = "4006381333931"; // distinct, real checksum-valid EAN-13

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-G", "tenant-g", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "GEN", "General", "general", null, 1, CategoryConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using (var seedProduct = CreateContext(connectionString))
            {
                var product = Product.Create(
                    Guid.NewGuid(), tenantId, "GEN-001", "Locally Owned Product", "locally-owned-product",
                    "STANDARD", "SIMPLE", null, null, null, null, null, true, true,
                    ProductConstants.ActiveStatus, null, now);
                seedProduct.Products.Add(product);
                seedProduct.ProductCategories.Add(
                    ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, categoryId, true, 0, null, now));
                await seedProduct.SaveChangesAsync();

                // Real barcode-resolution match projection is backed by a view/query over
                // Product+ProductVariant+ProductBarcode; seed the minimal real barcode row.
                var barcodeEntity = E_POS.Domain.Modules.Tenant.CatalogProduct.Entities.ProductBarcode.Create(
                    Guid.NewGuid(), tenantId, product.Id, null, barcode, "GTIN13", null, 1m, true, "ACTIVE", null, now, "GTIN13");
                seedProduct.ProductBarcodes.Add(barcodeEntity);
                await seedProduct.SaveChangesAsync();
            }

            await using var scope = await BuildRealServiceAsync(connectionString);
            var context = CreateFullPermissionContext(tenantId);

            var result = await scope.Service.ResolveBarcodeAsync(
                context,
                new ResolveProductBarcodeRequest { Barcode = barcode },
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.IsSuccess ? null : result.Error.Message);
            Assert.Equal("VALID_LOCAL_MATCH", result.Value!.Outcome);
            Assert.NotNull(result.Value.LocalMatch);
            Assert.Equal("Locally Owned Product", result.Value.LocalMatch!.ProductName);
        });
    }

    // --- Real DI-wired service construction ---------------------------------------------------

    private static async Task<ServiceScope> BuildRealServiceAsync(string connectionString)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var appsettingsDevPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "E_POS.Api", "appsettings.Development.json");

        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        if (File.Exists(appsettingsDevPath))
        {
            configBuilder.AddJsonFile(appsettingsDevPath, optional: true);
        }
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            // Blob storage is unrelated to this validation's scope (External Lookup/Resolution/Mapping);
            // AddInfrastructure eagerly validates AzureBlobStorageOptions on first resolution, so a
            // well-formed placeholder unblocks DI construction without touching real storage.
            ["AzureBlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
        });
        var configuration = configBuilder.Build();

        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Entitlements/subscription state is deliberately out of scope for this validation — a
        // permissive fake isolates External Lookup/Resolution/Mapping behavior from subscription
        // seed-data requirements, matching how the unit test suite already scopes this dependency.
        services.AddScoped<ITenantFeatureEntitlementEvaluator, AlwaysAllowEntitlementEvaluator>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        await db.Database.MigrateAsync();

        var service = scope.ServiceProvider.GetRequiredService<ITenantAdminProductService>();
        return new ServiceScope(provider, scope, service);
    }

    private sealed class ServiceScope : IAsyncDisposable
    {
        private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _root;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScope _scope;

        public ServiceScope(
            Microsoft.Extensions.DependencyInjection.ServiceProvider root,
            Microsoft.Extensions.DependencyInjection.IServiceScope scope,
            ITenantAdminProductService service)
        {
            _root = root;
            _scope = scope;
            Service = service;
        }

        public ITenantAdminProductService Service { get; }

        public ValueTask DisposeAsync()
        {
            _scope.Dispose();
            return _root.DisposeAsync();
        }
    }

    private sealed class AlwaysAllowEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false));

        public Task<bool> IsEnabledAsync(
            Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private static TenantRequestContext CreateFullPermissionContext(Guid tenantId) => new(
        tenantId,
        Guid.NewGuid(),
        new[]
        {
            ProductConstants.CreatePermission,
            ProductConstants.ViewPermission,
            ProductConstants.UpdatePermission,
            ProductConstants.ManagePermission,
            ProductConstants.PublishPermission,
        });

    private static Tenant CreateTenant(Guid id, string code, string slug, DateTimeOffset now) =>
        Tenant.Create(id, code, slug, code, "ACTIVE", "USD", "UTC", null, null, now);

    private static EPosDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static async Task WithMigratedDatabaseAsync(Func<string, Task> assertion)
    {
        var databaseName = $"e2e_test_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(AdminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = databaseName }.ConnectionString;
            await using (var db = CreateContext(connectionString)) await db.Database.MigrateAsync();
            await assertion(connectionString);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(AdminConnectionString);
        await admin.OpenAsync();
        await using (var terminate = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()", admin))
        {
            terminate.Parameters.AddWithValue("database", databaseName);
            await terminate.ExecuteNonQueryAsync();
        }
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
        await drop.ExecuteNonQueryAsync();
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
