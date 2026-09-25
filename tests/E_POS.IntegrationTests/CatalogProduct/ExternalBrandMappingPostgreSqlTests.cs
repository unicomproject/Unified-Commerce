using E_POS.Application.Common.Contracts;
using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class ExternalBrandMappingPostgreSqlTests
{
    private static readonly string AdminConnectionString =
        new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION_STRING must be set."))
        { Database = "postgres" }.ConnectionString;

    [Fact]
    public async Task CleanMigration_ExposesTenantSafeConstraintsAndIndexes_OnPostgreSql()
    {
        var databaseName = $"ebm_mig_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(AdminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = databaseName }.ConnectionString;
            await using var db = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);
            await db.Database.MigrateAsync();

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = """
                SELECT
                  EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='external_brand_mappings'),
                  to_regclass('public.uq_external_brand_mappings_tenant_provider_key') IS NOT NULL,
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_external_brand_mappings_tenant_id_tenants'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_external_brand_mappings_tenant_brand'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_external_brand_mappings_tenant_brand' AND confdeltype='r')
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            for (var index = 0; index < 5; index++) Assert.True(reader.GetBoolean(index));

            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("_AddTenantExternalBrandMappings", StringComparison.Ordinal));
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task UniqueMapping_RejectsDuplicateKey_WithinSameTenant()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var brandId1 = Guid.NewGuid();
            var brandId2 = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Brands.AddRange(
                    Brand.Create(brandId1, tenantId, "BRAND1", "Brand 1", "brand-1", null, BrandConstants.ActiveStatus, null, now),
                    Brand.Create(brandId2, tenantId, "BRAND2", "Brand 2", "brand-2", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            var mapping1 = ExternalBrandMapping.Create(
                Guid.NewGuid(),
                tenantId,
                "openfoodfacts",
                "coca cola",
                "Coca-Cola",
                brandId1,
                "PRODUCT_CONFIRMED",
                null,
                now);

            context.ExternalBrandMappings.Add(mapping1);
            await context.SaveChangesAsync();

            var mappingDuplicate = ExternalBrandMapping.Create(
                Guid.NewGuid(),
                tenantId,
                "openfoodfacts",
                "coca cola",
                "Coca-Cola Alternate",
                brandId2,
                "PRODUCT_CONFIRMED",
                null,
                now);

            context.ExternalBrandMappings.Add(mappingDuplicate);

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.Contains("uq_external_brand_mappings_tenant_provider_key", ex.InnerException?.Message ?? ex.Message);
        });
    }

    [Fact]
    public async Task DifferentTenants_CanMapSameProviderAndKey_Independently()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var brandA = Guid.NewGuid();
            var brandB = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.AddRange(
                    CreateTenant(tenantA, "TENANT-A", "tenant-a", now),
                    CreateTenant(tenantB, "TENANT-B", "tenant-b", now));
                seed.Brands.AddRange(
                    Brand.Create(brandA, tenantA, "COCA-A", "Coca Cola A", "coca-cola-a", null, BrandConstants.ActiveStatus, null, now),
                    Brand.Create(brandB, tenantB, "COCA-B", "Coca Cola B", "coca-cola-b", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var contextA = CreateContext(connectionString);
            await using var contextB = CreateContext(connectionString);
            var repoA = CreateRepository(contextA);
            var repoB = CreateRepository(contextB);

            await repoA.UpsertAsync(tenantA, "openfoodfacts", "coca cola", "Coca-Cola", brandA, "PRODUCT_CONFIRMED", null, CancellationToken.None);
            await repoB.UpsertAsync(tenantB, "openfoodfacts", "coca cola", "Coca-Cola", brandB, "PRODUCT_CONFIRMED", null, CancellationToken.None);

            var retrievedA = await repoA.GetAsync(tenantA, "openfoodfacts", "coca cola", CancellationToken.None);
            var retrievedB = await repoB.GetAsync(tenantB, "openfoodfacts", "coca cola", CancellationToken.None);

            Assert.NotNull(retrievedA);
            Assert.Equal(brandA, retrievedA!.TenantBrandId);

            Assert.NotNull(retrievedB);
            Assert.Equal(brandB, retrievedB!.TenantBrandId);
        });
    }

    [Fact]
    public async Task CrossTenantBrandProtection_DatabaseRejects_MappingToOtherTenantBrand()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var brandB = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.AddRange(
                    CreateTenant(tenantA, "TENANT-A", "tenant-a", now),
                    CreateTenant(tenantB, "TENANT-B", "tenant-b", now));
                seed.Brands.Add(
                    Brand.Create(brandB, tenantB, "BRAND-B", "Brand B", "brand-b", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var contextA = CreateContext(connectionString);

            // Tenant A tries to map to a Brand belonging to Tenant B
            var crossTenantMapping = ExternalBrandMapping.Create(
                Guid.NewGuid(),
                tenantA,
                "openfoodfacts",
                "coca cola",
                "Coca-Cola",
                brandB,
                "PRODUCT_CONFIRMED",
                null,
                now);

            contextA.ExternalBrandMappings.Add(crossTenantMapping);

            // Must be rejected by PostgreSQL foreign key constraint (fk_external_brand_mappings_tenant_brand)
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => contextA.SaveChangesAsync());
            Assert.Contains("fk_external_brand_mappings_tenant_brand", ex.InnerException?.Message ?? ex.Message);
        });
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_UpdatesToNewBrand()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var brand1 = Guid.NewGuid();
            var brand2 = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Brands.AddRange(
                    Brand.Create(brand1, tenantId, "BRAND1", "Brand 1", "brand-1", null, BrandConstants.ActiveStatus, null, now),
                    Brand.Create(brand2, tenantId, "BRAND2", "Brand 2", "brand-2", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            var repo = CreateRepository(context);

            var mapping = await repo.UpsertAsync(
                tenantId,
                "openfoodfacts",
                "coca cola",
                "Coca-Cola",
                brand1,
                "PRODUCT_CONFIRMED",
                null,
                CancellationToken.None);

            Assert.Equal(brand1, mapping.TenantBrandId);

            // Re-upsert with brand2
            var updated = await repo.UpsertAsync(
                tenantId,
                "openfoodfacts",
                "coca cola",
                "Coca-Cola Updated",
                brand2,
                "PRODUCT_CONFIRMED",
                null,
                CancellationToken.None);

            Assert.Equal(mapping.Id, updated.Id);
            Assert.Equal(brand2, updated.TenantBrandId);
            Assert.Equal("Coca-Cola Updated", updated.ExternalBrandName);

            // Verify single row persisted in database
            await using var verifyContext = CreateContext(connectionString);
            var persisted = await verifyContext.ExternalBrandMappings.SingleAsync(x => x.TenantId == tenantId);
            Assert.Equal(brand2, persisted.TenantBrandId);
        });
    }

    [Fact]
    public async Task ProductCreation_WithBrandMappingContext_PersistsProductAndMapping_Atomically()
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
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now));
                seed.Brands.Add(
                    Brand.Create(brandId, tenantId, "COCA_COLA", "Coca Cola", "coca-cola", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            var mappingRepo = CreateRepository(context);

            await using var transaction = await context.Database.BeginTransactionAsync();

            var product = Product.Create(
                Guid.NewGuid(),
                tenantId,
                "COCA-330",
                "Coca-Cola 330ml",
                "coca-cola-330ml",
                "STANDARD",
                "SIMPLE",
                null,
                brandId,
                null,
                null,
                null,
                true,
                true,
                ProductConstants.ActiveStatus,
                null,
                now);
            context.Products.Add(product);
            context.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, categoryId, true, 0, null, now));

            await mappingRepo.UpsertAsync(
                tenantId,
                "openfoodfacts",
                "coca cola",
                "Coca-Cola",
                brandId,
                "PRODUCT_CONFIRMED",
                null,
                CancellationToken.None,
                saveChanges: false);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await using var verifyContext = CreateContext(connectionString);
            var savedProduct = await verifyContext.Products.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.ProductName == "Coca-Cola 330ml");
            Assert.NotNull(savedProduct);
            Assert.Equal(brandId, savedProduct!.BrandId);

            var savedMapping = await verifyContext.ExternalBrandMappings.SingleOrDefaultAsync(m => m.TenantId == tenantId && m.Provider == "openfoodfacts" && m.ExternalBrandKey == "coca cola");
            Assert.NotNull(savedMapping);
            Assert.Equal(brandId, savedMapping!.TenantBrandId);
            Assert.Equal("Coca-Cola", savedMapping.ExternalBrandName);
        });
    }

    [Fact]
    public async Task ProductCreation_WhenTransactionFails_RollsBackProductAndBrandMapping()
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
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now));
                seed.Brands.Add(
                    Brand.Create(brandId, tenantId, "COCA_COLA", "Coca Cola", "coca-cola", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using (var context = CreateContext(connectionString))
            {
                var mappingRepo = CreateRepository(context);

                await using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    var product = Product.Create(
                        Guid.NewGuid(),
                        tenantId,
                        "FAIL-01",
                        "Failing Product",
                        "failing-product",
                        "STANDARD",
                        "SIMPLE",
                        null,
                        brandId,
                        null,
                        null,
                        null,
                        true,
                        true,
                        ProductConstants.ActiveStatus,
                        null,
                        now);
                    context.Products.Add(product);
                    context.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, categoryId, true, 0, null, now));

                    await mappingRepo.UpsertAsync(
                        tenantId,
                        "openfoodfacts",
                        "coca cola",
                        "Coca-Cola",
                        brandId,
                        "PRODUCT_CONFIRMED",
                        null,
                        CancellationToken.None,
                        saveChanges: false);

                    await context.SaveChangesAsync();

                    // Force transaction rollback
                    await transaction.RollbackAsync();
                }
            }

            // Verify physically in PostgreSQL: product and mapping must be ABSENT
            await using var verifyContext = CreateContext(connectionString);
            var productExists = await verifyContext.Products.AnyAsync(p => p.TenantId == tenantId);
            var mappingExists = await verifyContext.ExternalBrandMappings.AnyAsync(m => m.TenantId == tenantId);

            Assert.False(productExists);
            Assert.False(mappingExists);
        });
    }

    [Fact]
    public async Task ProductCreation_WhenUpdatingExistingBrandMappingFails_RollsBackToOriginalMapping()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var brand1 = Guid.NewGuid();
            var brand2 = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now));
                seed.Brands.AddRange(
                    Brand.Create(brand1, tenantId, "BRAND1", "Brand 1", "brand-1", null, BrandConstants.ActiveStatus, null, now),
                    Brand.Create(brand2, tenantId, "BRAND2", "Brand 2", "brand-2", null, BrandConstants.ActiveStatus, null, now));
                seed.ExternalBrandMappings.Add(
                    ExternalBrandMapping.Create(Guid.NewGuid(), tenantId, "openfoodfacts", "coca cola", "Coca-Cola", brand1, "PRODUCT_CONFIRMED", null, now));
                await seed.SaveChangesAsync();
            }

            await using (var context = CreateContext(connectionString))
            {
                var mappingRepo = CreateRepository(context);

                await using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    var product = Product.Create(
                        Guid.NewGuid(),
                        tenantId,
                        "FAIL-OVERRIDE",
                        "Failing Override Product",
                        "failing-override-product",
                        "STANDARD",
                        "SIMPLE",
                        null,
                        brand2,
                        null,
                        null,
                        null,
                        true,
                        true,
                        ProductConstants.ActiveStatus,
                        null,
                        now);
                    context.Products.Add(product);
                    context.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, categoryId, true, 0, null, now));

                    await mappingRepo.UpsertAsync(
                        tenantId,
                        "openfoodfacts",
                        "coca cola",
                        "Coca-Cola Updated",
                        brand2,
                        "PRODUCT_CONFIRMED",
                        null,
                        CancellationToken.None,
                        saveChanges: false);

                    await context.SaveChangesAsync();

                    // Force transaction failure / rollback
                    await transaction.RollbackAsync();
                }
            }

            // Physical verify on PostgreSQL: mapping still points to brand1!
            await using var verifyContext = CreateContext(connectionString);
            var mapping = await verifyContext.ExternalBrandMappings.SingleAsync(m => m.TenantId == tenantId);
            Assert.Equal(brand1, mapping.TenantBrandId);
            Assert.Equal("Coca-Cola", mapping.ExternalBrandName);
        });
    }

    [Fact]
    public async Task ProductCreation_WithoutBrandMappingContext_SucceedsWithoutCreatingMapping()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "APPAREL", "Apparel", "apparel", null, 1, CategoryConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            await using var transaction = await context.Database.BeginTransactionAsync();

            // No BrandId on the product at all (Brand is optional, unlike Category).
            var product = Product.Create(
                Guid.NewGuid(),
                tenantId,
                "SHIRT-01",
                "Manual Shirt",
                "manual-shirt",
                "STANDARD",
                "SIMPLE",
                null,
                null,
                null,
                null,
                null,
                true,
                true,
                ProductConstants.ActiveStatus,
                null,
                now);
            context.Products.Add(product);
            context.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, categoryId, true, 0, null, now));

            // No brand mapping upsert staged
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Verify in PostgreSQL
            await using var verifyContext = CreateContext(connectionString);
            Assert.True(await verifyContext.Products.AnyAsync(p => p.TenantId == tenantId && p.ProductName == "Manual Shirt"));
            Assert.False(await verifyContext.ExternalBrandMappings.AnyAsync(m => m.TenantId == tenantId));
        });
    }

    [Fact]
    public async Task ProductCreation_WithCategoryAndBrandMappingContext_BothPersistInSameTransaction()
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
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.Add(
                    Category.Create(categoryId, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now));
                seed.Brands.Add(
                    Brand.Create(brandId, tenantId, "COCA_COLA", "Coca Cola", "coca-cola", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            var categoryMappingRepo = new ExternalCategoryMappingRepository(context, new TestDateTimeProvider(), NullLogger<ExternalCategoryMappingRepository>.Instance);
            var brandMappingRepo = CreateRepository(context);

            await using var transaction = await context.Database.BeginTransactionAsync();

            var product = Product.Create(
                Guid.NewGuid(),
                tenantId,
                "COCA-330",
                "Coca-Cola 330ml",
                "coca-cola-330ml",
                "STANDARD",
                "SIMPLE",
                null,
                brandId,
                null,
                null,
                null,
                true,
                true,
                ProductConstants.ActiveStatus,
                null,
                now);
            context.Products.Add(product);
            context.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, categoryId, true, 0, null, now));

            await categoryMappingRepo.UpsertAsync(
                tenantId, "openfoodfacts", "en:colas", "Colas", categoryId, "PRODUCT_CONFIRMED", null, CancellationToken.None, saveChanges: false);
            await brandMappingRepo.UpsertAsync(
                tenantId, "openfoodfacts", "coca cola", "Coca-Cola", brandId, "PRODUCT_CONFIRMED", null, CancellationToken.None, saveChanges: false);

            // Single SaveChangesAsync stages Product + both mappings atomically.
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await using var verifyContext = CreateContext(connectionString);
            Assert.True(await verifyContext.Products.AnyAsync(p => p.TenantId == tenantId));
            Assert.True(await verifyContext.ExternalCategoryMappings.AnyAsync(m => m.TenantId == tenantId && m.ExternalCategoryKey == "en:colas"));
            Assert.True(await verifyContext.ExternalBrandMappings.AnyAsync(m => m.TenantId == tenantId && m.ExternalBrandKey == "coca cola"));
        });
    }

    private static ExternalBrandMappingRepository CreateRepository(EPosDbContext context) =>
        new(context, new TestDateTimeProvider(), NullLogger<ExternalBrandMappingRepository>.Instance);

    private static Tenant CreateTenant(Guid id, string code, string slug, DateTimeOffset now) =>
        Tenant.Create(id, code, slug, code, "ACTIVE", "USD", "UTC", null, null, now);

    private static EPosDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static async Task WithMigratedDatabaseAsync(Func<string, Task> assertion)
    {
        var databaseName = $"ebm_test_{Guid.NewGuid():N}";
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
