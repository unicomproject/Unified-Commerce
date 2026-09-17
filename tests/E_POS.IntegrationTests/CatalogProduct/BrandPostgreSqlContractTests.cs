using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class BrandPostgreSqlContractTests
{
    private static readonly string AdminConnectionString =
        new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=SNirosh1985")
        { Database = "postgres" }.ConnectionString;

    [Fact]
    public async Task CleanMigration_ExposesBrandConcurrencyAndTenantSafeConstraints_OnPostgreSql()
    {
        var databaseName = $"brand_p1_{Guid.NewGuid():N}";
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
                  EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='brands' AND column_name='row_version' AND data_type='bigint'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_brands_row_version'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_brands_sort_order'),
                  to_regclass('public.uq_brands_tenant_id_brand_code') IS NOT NULL,
                  to_regclass('public.uq_brands_tenant_id_brand_slug') IS NOT NULL,
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_products_brand_tenant'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_brands_logo_media_asset_tenant'),
                  EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_products_brand_tenant' AND confdeltype='r'),
                  EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='brands' AND column_name='row_version' AND column_default='1')
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            for (var index = 0; index < 9; index++) Assert.True(reader.GetBoolean(index));

            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("_ImplementTenantAdminBrandContract", StringComparison.Ordinal));
            Assert.Contains(applied, migration => migration.EndsWith("_AddBrandOptimisticConcurrency", StringComparison.Ordinal));
        }
        finally
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
    }

    [Fact]
    public async Task CompetingBrandWrites_TranslateCodeAndSlugConflicts_AndRemainTenantScoped()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.AddRange(
                    CreateTenant(tenantA, "RACE-A", "race-a", now),
                    CreateTenant(tenantB, "RACE-B", "race-b", now));
                await seed.SaveChangesAsync();
            }

            await using var contextA = CreateContext(connectionString);
            await using var contextB = CreateContext(connectionString);
            var repositoryA = new BrandRepository(contextA);
            var repositoryB = new BrandRepository(contextB);

            Assert.False(await repositoryA.BrandCodeExistsAsync(tenantA, "NIKE", null, CancellationToken.None));
            Assert.False(await repositoryB.BrandCodeExistsAsync(tenantA, "NIKE", null, CancellationToken.None));

            var brandA = CreateBrand(tenantA, "NIKE", "nike", "Writer A", now);
            var brandB = CreateBrand(tenantA, "NIKE", "nike", "Writer B", now);
            var outcomes = await Task.WhenAll(CaptureAddAsync(repositoryA, brandA), CaptureAddAsync(repositoryB, brandB));

            Assert.Equal(1, outcomes.Count(x => x is null));
            var codeConflict = Assert.Single(outcomes.OfType<BrandPersistenceException>());
            Assert.Equal("brand.code_conflict", codeConflict.ErrorCode);

            await using (var verify = CreateContext(connectionString))
            {
                Assert.Equal(1, await verify.Brands.CountAsync(x => x.TenantId == tenantA && x.BrandCode == "NIKE"));
            }

            await using (var slugOne = CreateContext(connectionString))
            await using (var slugTwo = CreateContext(connectionString))
            {
                await new BrandRepository(slugOne).AddAsync(CreateBrand(tenantA, "SLUG-A", "shared-slug", "Slug A", now), CancellationToken.None);
                var slugError = await Assert.ThrowsAsync<BrandPersistenceException>(() =>
                    new BrandRepository(slugTwo).AddAsync(CreateBrand(tenantA, "SLUG-B", "shared-slug", "Slug B", now), CancellationToken.None));
                Assert.Equal("brand.slug_conflict", slugError.ErrorCode);
            }

            await using (var crossTenant = CreateContext(connectionString))
            {
                await new BrandRepository(crossTenant).AddAsync(CreateBrand(tenantB, "NIKE", "nike", "Tenant B Nike", now), CancellationToken.None);
            }

            await using var final = CreateContext(connectionString);
            Assert.Equal(2, await final.Brands.CountAsync(x => x.BrandCode == "NIKE"));
        });
    }

    [Fact]
    public async Task TwoDbContexts_StaleBrandWrite_IsTranslatedAndPreservesNewerValue()
    {
        await WithMigratedDatabaseAsync(async connectionString =>
        {
            var now = DateTimeOffset.UtcNow;
            var tenantId = Guid.NewGuid();
            var brandId = Guid.NewGuid();
            await using (var seed = CreateContext(connectionString))
            {
                seed.Currencies.Add(Currency.Create(Guid.NewGuid(), "USD", "US Dollar", "$", 2, true, 0, now));
                seed.Tenants.Add(CreateTenant(tenantId, "STALE", "stale", now));
                seed.Brands.Add(Brand.Create(brandId, tenantId, "NIKE", "Nike", "nike", null, BrandConstants.ActiveStatus, null, now));
                await seed.SaveChangesAsync();
            }

            await using var contextA = CreateContext(connectionString);
            await using var contextB = CreateContext(connectionString);
            var repositoryA = new BrandRepository(contextA);
            var repositoryB = new BrandRepository(contextB);
            var staleA = await repositoryA.GetEditableAsync(tenantId, brandId, CancellationToken.None);
            var currentB = await repositoryB.GetEditableAsync(tenantId, brandId, CancellationToken.None);
            Assert.NotNull(staleA);
            Assert.NotNull(currentB);
            Assert.Equal(1, staleA!.RowVersion);
            Assert.Equal(1, currentB!.RowVersion);

            currentB.UpdateProfile("NIKE", "Nike Updated By B", "nike", null, BrandConstants.ActiveStatus, null, now.AddMinutes(1));
            await repositoryB.SaveChangesAsync(CancellationToken.None);
            Assert.Equal(2, currentB.RowVersion);

            staleA.UpdateProfile("NIKE", "Nike Updated By A", "nike", null, BrandConstants.ActiveStatus, null, now.AddMinutes(2));
            var error = await Assert.ThrowsAsync<BrandPersistenceException>(() => repositoryA.SaveChangesAsync(CancellationToken.None));
            Assert.Equal("brand.concurrency_conflict", error.ErrorCode);

            await using var verify = CreateContext(connectionString);
            var persisted = await verify.Brands.AsNoTracking().SingleAsync(x => x.Id == brandId && x.TenantId == tenantId);
            Assert.Equal("Nike Updated By B", persisted.BrandName);
            Assert.Equal(2, persisted.RowVersion);
        });
    }

    private static async Task<Exception?> CaptureAddAsync(BrandRepository repository, Brand brand)
    {
        try
        {
            await repository.AddAsync(brand, CancellationToken.None);
            return null;
        }
        catch (BrandPersistenceException exception)
        {
            return exception;
        }
    }

    private static Brand CreateBrand(Guid tenantId, string code, string slug, string name, DateTimeOffset now) =>
        Brand.Create(Guid.NewGuid(), tenantId, code, name, slug, null, BrandConstants.ActiveStatus, null, now);

    private static Tenant CreateTenant(Guid id, string code, string slug, DateTimeOffset now) =>
        Tenant.Create(id, code, slug, code, "ACTIVE", "USD", "UTC", null, null, now);

    private static EPosDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static async Task WithMigratedDatabaseAsync(Func<string, Task> assertion)
    {
        var databaseName = $"brand_verify_{Guid.NewGuid():N}";
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
    }
}
