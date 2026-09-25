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

public sealed class ExternalCategoryMappingPostgreSqlTests
{
    private static readonly string AdminConnectionString =
        new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION_STRING must be set."))
        { Database = "postgres" }.ConnectionString;

    [Fact]
    public async Task CleanMigration_ExposesTenantSafeConstraintsAndIndexes_OnPostgreSql()
    {
        var databaseName = $"ecm_mig_{Guid.NewGuid():N}";
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
                  EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='external_category_mappings'),
                  to_regclass('public.uq_external_category_mappings_tenant_provider_key') IS NOT NULL,
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_external_category_mappings_tenant_id_tenants'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_external_category_mappings_tenant_category'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_external_category_mappings_tenant_category' AND confdeltype='r')
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            for (var index = 0; index < 5; index++) Assert.True(reader.GetBoolean(index));

            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("_AddTenantExternalCategoryMappings", StringComparison.Ordinal));
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
            var categoryId1 = Guid.NewGuid();
            var categoryId2 = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.AddRange(
                    Category.Create(categoryId1, tenantId, null, "CAT1", "Category 1", "cat-1", null, 1, CategoryConstants.ActiveStatus, null, now),
                    Category.Create(categoryId2, tenantId, null, "CAT2", "Category 2", "cat-2", null, 2, CategoryConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            var mapping1 = ExternalCategoryMapping.Create(
                Guid.NewGuid(),
                tenantId,
                "openfoodfacts",
                "en:colas",
                "Colas",
                categoryId1,
                "PRODUCT_CONFIRMED",
                null,
                now);

            context.ExternalCategoryMappings.Add(mapping1);
            await context.SaveChangesAsync();

            var mappingDuplicate = ExternalCategoryMapping.Create(
                Guid.NewGuid(),
                tenantId,
                "openfoodfacts",
                "en:colas",
                "Colas Alternate",
                categoryId2,
                "PRODUCT_CONFIRMED",
                null,
                now);

            context.ExternalCategoryMappings.Add(mappingDuplicate);

            // Attempting to persist duplicate mapping within same tenant must fail with DbUpdateException / unique constraint violation
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.Contains("uq_external_category_mappings_tenant_provider_key", ex.InnerException?.Message ?? ex.Message);
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
            var categoryA = Guid.NewGuid();
            var categoryB = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.AddRange(
                    CreateTenant(tenantA, "TENANT-A", "tenant-a", now),
                    CreateTenant(tenantB, "TENANT-B", "tenant-b", now));
                seed.Categories.AddRange(
                    Category.Create(categoryA, tenantA, null, "BEV-A", "Beverages A", "bev-a", null, 1, CategoryConstants.ActiveStatus, null, now),
                    Category.Create(categoryB, tenantB, null, "BEV-B", "Beverages B", "bev-b", null, 1, CategoryConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var contextA = CreateContext(connectionString);
            await using var contextB = CreateContext(connectionString);
            var repoA = CreateRepository(contextA);
            var repoB = CreateRepository(contextB);

            await repoA.UpsertAsync(tenantA, "openfoodfacts", "en:colas", "Colas", categoryA, "PRODUCT_CONFIRMED", null, CancellationToken.None);
            await repoB.UpsertAsync(tenantB, "openfoodfacts", "en:colas", "Colas", categoryB, "PRODUCT_CONFIRMED", null, CancellationToken.None);

            var retrievedA = await repoA.GetAsync(tenantA, "openfoodfacts", "en:colas", CancellationToken.None);
            var retrievedB = await repoB.GetAsync(tenantB, "openfoodfacts", "en:colas", CancellationToken.None);

            Assert.NotNull(retrievedA);
            Assert.Equal(categoryA, retrievedA!.TenantCategoryId);

            Assert.NotNull(retrievedB);
            Assert.Equal(categoryB, retrievedB!.TenantCategoryId);
        });
    }

    [Fact]
    public async Task CrossTenantCategoryProtection_DatabaseRejects_MappingToOtherTenantCategory()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var categoryB = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.AddRange(
                    CreateTenant(tenantA, "TENANT-A", "tenant-a", now),
                    CreateTenant(tenantB, "TENANT-B", "tenant-b", now));
                seed.Categories.Add(
                    Category.Create(categoryB, tenantB, null, "CAT-B", "Category B", "cat-b", null, 1, CategoryConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var contextA = CreateContext(connectionString);

            // Tenant A tries to map to a Category belonging to Tenant B
            var crossTenantMapping = ExternalCategoryMapping.Create(
                Guid.NewGuid(),
                tenantA,
                "openfoodfacts",
                "en:colas",
                "Colas",
                categoryB,
                "PRODUCT_CONFIRMED",
                null,
                now);

            contextA.ExternalCategoryMappings.Add(crossTenantMapping);

            // Must be rejected by PostgreSQL foreign key constraint (fk_external_category_mappings_tenant_category)
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => contextA.SaveChangesAsync());
            Assert.Contains("fk_external_category_mappings_tenant_category", ex.InnerException?.Message ?? ex.Message);
        });
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_UpdatesToNewCategory()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var category1 = Guid.NewGuid();
            var category2 = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.AddRange(
                    Category.Create(category1, tenantId, null, "CAT1", "Category 1", "cat-1", null, 1, CategoryConstants.ActiveStatus, null, now),
                    Category.Create(category2, tenantId, null, "CAT2", "Category 2", "cat-2", null, 2, CategoryConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var context = CreateContext(connectionString);
            var repo = CreateRepository(context);

            var mapping = await repo.UpsertAsync(
                tenantId,
                "openfoodfacts",
                "en:colas",
                "Colas",
                category1,
                "PRODUCT_CONFIRMED",
                null,
                CancellationToken.None);

            Assert.Equal(category1, mapping.TenantCategoryId);

            // Re-upsert with category2
            var updated = await repo.UpsertAsync(
                tenantId,
                "openfoodfacts",
                "en:colas",
                "Colas Updated",
                category2,
                "PRODUCT_CONFIRMED",
                null,
                CancellationToken.None);

            Assert.Equal(mapping.Id, updated.Id);
            Assert.Equal(category2, updated.TenantCategoryId);
            Assert.Equal("Colas Updated", updated.ExternalCategoryName);

            // Verify single row persisted in database
            await using var verifyContext = CreateContext(connectionString);
            var persisted = await verifyContext.ExternalCategoryMappings.SingleAsync(x => x.TenantId == tenantId);
            Assert.Equal(category2, persisted.TenantCategoryId);
        });
    }

    [Fact]
    public async Task ProductCreation_WithMappingContext_PersistsProductAndMapping_Atomically()
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
                    Category.Create(categoryId, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now));
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

            await mappingRepo.UpsertAsync(
                tenantId,
                "openfoodfacts",
                "en:colas",
                "Colas",
                categoryId,
                "PRODUCT_CONFIRMED",
                null,
                CancellationToken.None,
                saveChanges: false);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await using var verifyContext = CreateContext(connectionString);
            var savedProduct = await verifyContext.Products.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.ProductName == "Coca-Cola 330ml");
            Assert.NotNull(savedProduct);

            var savedProductCategory = await verifyContext.ProductCategories.SingleOrDefaultAsync(pc => pc.TenantId == tenantId && pc.ProductId == product.Id);
            Assert.NotNull(savedProductCategory);
            Assert.Equal(categoryId, savedProductCategory!.CategoryId);

            var savedMapping = await verifyContext.ExternalCategoryMappings.SingleOrDefaultAsync(m => m.TenantId == tenantId && m.Provider == "openfoodfacts" && m.ExternalCategoryKey == "en:colas");
            Assert.NotNull(savedMapping);
            Assert.Equal(categoryId, savedMapping!.TenantCategoryId);
            Assert.Equal("Colas", savedMapping.ExternalCategoryName);
        });
    }

    [Fact]
    public async Task ProductCreation_WhenTransactionFails_RollsBackProductAndMapping()
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
                    Category.Create(categoryId, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now));
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

                    await mappingRepo.UpsertAsync(
                        tenantId,
                        "openfoodfacts",
                        "en:colas",
                        "Colas",
                        categoryId,
                        "PRODUCT_CONFIRMED",
                        null,
                        CancellationToken.None,
                        saveChanges: false);

                    await context.SaveChangesAsync();

                    // Force transaction rollback
                    await transaction.RollbackAsync();
                }
            }

            // Verify physically in PostgreSQL: all 3 must be ABSENT
            await using var verifyContext = CreateContext(connectionString);
            var productExists = await verifyContext.Products.AnyAsync(p => p.TenantId == tenantId);
            var categoryExists = await verifyContext.ProductCategories.AnyAsync(pc => pc.TenantId == tenantId);
            var mappingExists = await verifyContext.ExternalCategoryMappings.AnyAsync(m => m.TenantId == tenantId);

            Assert.False(productExists);
            Assert.False(categoryExists);
            Assert.False(mappingExists);
        });
    }

    [Fact]
    public async Task ProductCreation_WhenUpdatingExistingMappingFails_RollsBackToOriginalMapping()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var category1 = Guid.NewGuid();
            var category2 = Guid.NewGuid();

            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "TENANT-1", "tenant-1", now));
                seed.Categories.AddRange(
                    Category.Create(category1, tenantId, null, "SOFT-DRINKS", "Soft Drinks", "soft-drinks", null, 1, CategoryConstants.ActiveStatus, null, now),
                    Category.Create(category2, tenantId, null, "BEVERAGES", "Beverages", "beverages", null, 2, CategoryConstants.ActiveStatus, null, now));
                seed.ExternalCategoryMappings.Add(
                    ExternalCategoryMapping.Create(Guid.NewGuid(), tenantId, "openfoodfacts", "en:colas", "Colas", category1, "PRODUCT_CONFIRMED", null, now));
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
                    context.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, product.Id, category2, true, 0, null, now));

                    await mappingRepo.UpsertAsync(
                        tenantId,
                        "openfoodfacts",
                        "en:colas",
                        "Colas Updated",
                        category2,
                        "PRODUCT_CONFIRMED",
                        null,
                        CancellationToken.None,
                        saveChanges: false);

                    await context.SaveChangesAsync();

                    // Force transaction failure / rollback
                    await transaction.RollbackAsync();
                }
            }

            // Physical verify on PostgreSQL: mapping still points to category1!
            await using var verifyContext = CreateContext(connectionString);
            var mapping = await verifyContext.ExternalCategoryMappings.SingleAsync(m => m.TenantId == tenantId);
            Assert.Equal(category1, mapping.TenantCategoryId);
            Assert.Equal("Colas", mapping.ExternalCategoryName);
        });
    }

    [Fact]
    public async Task ProductCreation_WithoutMappingContext_SucceedsWithoutCreatingMapping()
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

            // No mapping upsert staged
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Verify in PostgreSQL
            await using var verifyContext = CreateContext(connectionString);
            Assert.True(await verifyContext.Products.AnyAsync(p => p.TenantId == tenantId && p.ProductName == "Manual Shirt"));
            Assert.False(await verifyContext.ExternalCategoryMappings.AnyAsync(m => m.TenantId == tenantId));
        });
    }

    private static ExternalCategoryMappingRepository CreateRepository(EPosDbContext context) =>
        new(context, new TestDateTimeProvider(), NullLogger<ExternalCategoryMappingRepository>.Instance);

    private static Tenant CreateTenant(Guid id, string code, string slug, DateTimeOffset now) =>
        Tenant.Create(id, code, slug, code, "ACTIVE", "USD", "UTC", null, null, now);

    private static EPosDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static async Task WithMigratedDatabaseAsync(Func<string, Task> assertion)
    {
        var databaseName = $"ecm_test_{Guid.NewGuid():N}";
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
