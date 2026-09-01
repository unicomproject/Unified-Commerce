using E_POS.Api.Middleware;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class CategoryPostgreSqlTests
{
    private static readonly string[] CandidateConnectionStrings =
    [
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin",
        "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123"
    ];

    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Preflight_DuplicateCodeSameTenant_StopsSafely()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_code", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();
        var departmentOne = Guid.NewGuid();
        var departmentTwo = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, firstId, tenantId, departmentOne, "DRINK", "Drinks");
        await InsertLegacyCategoryAsync(db, secondId, tenantId, departmentTwo, "DRINK", "Sodas");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Equal("P0001", exception.SqlState);
        Assert.Contains("CAT-MIG-PREFLIGHT-001", exception.MessageText);
        Assert.Contains("duplicate normalized category_code", exception.MessageText);

        var remaining = await CountLegacyRowsAsync(db);
        Assert.Equal(2, remaining);
        Assert.Equal(firstId, await GetLegacyIdAsync(db, "DRINK", departmentOne));
        Assert.Equal(secondId, await GetLegacyIdAsync(db, "DRINK", departmentTwo));
    }

    [Fact]
    public async Task Preflight_DuplicateNameSameTenantIncludingWhitespace_StopsSafely()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_name", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, Guid.NewGuid(), tenantId, Guid.NewGuid(), "BEV", "Beverages");
        await InsertLegacyCategoryAsync(db, Guid.NewGuid(), tenantId, Guid.NewGuid(), "BEV2", " beverages ");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Equal("P0001", exception.SqlState);
        Assert.Contains("duplicate normalized category_name", exception.MessageText);
        Assert.Equal(2, await CountLegacyRowsAsync(db));
    }

    [Fact]
    public async Task Preflight_SameValuesAcrossTenants_AreAllowed()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_tenants", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DRINK", "Beverages");
        await InsertLegacyCategoryAsync(db, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DRINK", "Beverages");

        await db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql());
        Assert.Equal(2, await CountLegacyRowsAsync(db));
    }

    [Fact]
    public async Task Preflight_DanglingParent_StopsSafelyWithoutRepair()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category hierarchy preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_dangling", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var danglingParentId = Guid.NewGuid();

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, categoryId, tenantId, Guid.NewGuid(), "CHILD", "Child", danglingParentId);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Contains("DANGLING_PARENT", exception.MessageText);
        Assert.Contains(categoryId.ToString(), exception.MessageText);
        Assert.Equal(categoryId, await GetLegacyIdAsync(db, "CHILD", await GetLegacyDepartmentAsync(db, "CHILD")));
        Assert.Equal(danglingParentId, await GetLegacyParentAsync(db, categoryId));
    }

    [Fact]
    public async Task Preflight_CrossTenantParent_StopsSafely()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category hierarchy preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_xtenant", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, parentId, tenantA, Guid.NewGuid(), "PARENT", "Parent");
        await InsertLegacyCategoryAsync(db, childId, tenantB, Guid.NewGuid(), "CHILD", "Child", parentId);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Contains("CROSS_TENANT_PARENT", exception.MessageText);
        Assert.Equal(2, await CountLegacyRowsAsync(db));
        Assert.Equal(parentId, await GetLegacyParentAsync(db, childId));
    }

    [Fact]
    public async Task Preflight_SelfParent_StopsSafely()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category hierarchy preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_self", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, categoryId, tenantId, Guid.NewGuid(), "SELF", "Self", categoryId);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Contains("SELF_PARENT", exception.MessageText);
        Assert.Equal(categoryId, await GetLegacyParentAsync(db, categoryId));
    }

    [Fact]
    public async Task Preflight_Cycle_StopsSafely()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category hierarchy preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_cycle", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        await CreateLegacyCategoriesTableAsync(db);
        await InsertLegacyCategoryAsync(db, firstId, tenantId, Guid.NewGuid(), "ONE", "One", secondId);
        await InsertLegacyCategoryAsync(db, secondId, tenantId, Guid.NewGuid(), "TWO", "Two", firstId);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Contains("PARENT_CYCLE", exception.MessageText);
        Assert.Equal(firstId, await GetLegacyParentAsync(db, secondId));
        Assert.Equal(secondId, await GetLegacyParentAsync(db, firstId));
    }

    [Fact]
    public async Task Preflight_DepthSix_StopsSafely()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category hierarchy preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_depth", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();

        await CreateLegacyCategoriesTableAsync(db);
        for (var i = 0; i < ids.Length; i++)
        {
            await InsertLegacyCategoryAsync(
                db,
                ids[i],
                tenantId,
                Guid.NewGuid(),
                $"L{i + 1}",
                $"Level {i + 1}",
                i == 0 ? null : ids[i - 1]);
        }

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql()));

        Assert.Contains("MAX_DEPTH_EXCEEDED", exception.MessageText);
        Assert.Equal(6, await CountLegacyRowsAsync(db));
        Assert.Equal(ids[4], await GetLegacyParentAsync(db, ids[5]));
    }

    [Fact]
    public async Task Preflight_ValidDepthFive_Passes()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category hierarchy preflight verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_preflight_ok", ensureCreated: false);
        await using var db = CreateDb(harness.ConnectionString);
        var tenantId = Guid.NewGuid();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        await CreateLegacyCategoriesTableAsync(db);
        for (var i = 0; i < ids.Length; i++)
        {
            await InsertLegacyCategoryAsync(
                db,
                ids[i],
                tenantId,
                Guid.NewGuid(),
                $"OK{i + 1}",
                $"Ok {i + 1}",
                i == 0 ? null : ids[i - 1]);
        }

        await db.Database.ExecuteSqlRawAsync(CategoryMigrationPreflight.BuildGuardSql());
        Assert.Equal(5, await CountLegacyRowsAsync(db));
        Assert.Equal(ids[3], await GetLegacyParentAsync(db, ids[4]));
    }

    [Fact]
    public async Task TargetSchema_EnforcesTenantWideUniquenessDepthAndDescription()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category PostgreSQL schema verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_target_schema");
        await using var db = CreateDb(harness.ConnectionString);
        await db.Database.EnsureCreatedAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await SeedTenantsAsync(db, tenantA, tenantB);

        db.Categories.Add(Category.Create(rootId, tenantA, null, "FOOD", "Food", "food", null, 0, CategoryConstants.ActiveStatus, null, Now));
        db.Categories.Add(Category.Create(childId, tenantA, rootId, "MILK", "Milk", "milk", "Fresh milk", 1, CategoryConstants.ActiveStatus, null, Now));
        db.Categories.Add(Category.Create(Guid.NewGuid(), tenantB, null, "FOOD", "Food", "food", null, 0, CategoryConstants.ActiveStatus, null, Now));
        await db.SaveChangesAsync();

        var departmentColumnCount = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM information_schema.columns
                WHERE table_name = 'categories' AND column_name = 'department_id'
                """)
            .SingleAsync();
        Assert.Equal(0, departmentColumnCount);

        var codeUnique = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM pg_indexes
                WHERE tablename = 'categories'
                  AND indexname = 'uq_categories_tenant_id_category_code'
                """)
            .SingleAsync();
        Assert.Equal(1, codeUnique);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS uq_categories_tenant_id_normalized_category_name
            ON categories (tenant_id, LOWER(BTRIM(category_name)));
            """);

        var duplicateCode = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            db.Categories.Add(Category.Create(Guid.NewGuid(), tenantA, null, "FOOD", "Other Food", "other-food", null, 2, CategoryConstants.DeletedStatus, null, Now));
            await db.SaveChangesAsync();
        });
        Assert.Contains("uq_categories_tenant_id_category_code", duplicateCode.InnerException?.Message ?? duplicateCode.Message, StringComparison.OrdinalIgnoreCase);
        var mappedDuplicateCode = DatabaseExceptionMapper.Map(duplicateCode);
        Assert.Equal("category.duplicate_code", mappedDuplicateCode.Code);
        Assert.Equal(409, mappedDuplicateCode.StatusCode);
        db.ChangeTracker.Clear();

        var duplicateName = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO categories (
                    id, tenant_id, parent_category_id, category_code, category_name, category_slug,
                    sort_order, status, created_at, updated_at)
                VALUES (
                    {0}, {1}, NULL, 'BEV', ' FOOD ', 'beverages',
                    3, 'DELETED', {2}, {2});
                """,
                Guid.NewGuid(),
                tenantA,
                Now);
        });
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateName.SqlState);

        var mappedDuplicateName = DatabaseExceptionMapper.Map(duplicateName);
        Assert.Equal("category.duplicate_name", mappedDuplicateName.Code);
        Assert.Equal(409, mappedDuplicateName.StatusCode);

        var descriptionTooLong = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO categories (
                    id, tenant_id, parent_category_id, category_code, category_name, category_slug,
                    description, sort_order, status, created_at, updated_at)
                VALUES (
                    {0}, {1}, NULL, 'DESC', 'Description', 'description',
                    {2}, 4, 'ACTIVE', {3}, {3});
                """,
                Guid.NewGuid(),
                tenantA,
                new string('D', 2001),
                Now);
        });
        Assert.Contains(descriptionTooLong.SqlState, new[] { PostgresErrorCodes.CheckViolation, PostgresErrorCodes.StringDataRightTruncation });

        var idsPreservedRoot = await db.Categories.AsNoTracking().AnyAsync(x => x.Id == rootId);
        Assert.True(idsPreservedRoot);
        Assert.True(await db.Categories.AsNoTracking().AnyAsync(x => x.Id == childId && x.ParentCategoryId == rootId));

        var parentFk = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM pg_constraint
                WHERE conrelid = 'categories'::regclass
                  AND conname = 'fk_categories_tenant_parent_category'
                """)
            .SingleAsync();
        Assert.Equal(1, parentFk);
    }

    [Fact]
    public async Task TargetSchema_PreservesProductCategoryMappingsAndMediaFk()
    {
        var baseConnectionString = await TryConnectAsync();
        if (baseConnectionString is null)
        {
            Assert.Fail("PostgreSQL is required for Category mapping preservation verification.");
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConnectionString, "cat_mapping_preserve");
        await using var db = CreateDb(harness.ConnectionString);
        await db.Database.EnsureCreatedAsync();

        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();

        await SeedTenantsAsync(db, tenantId);

        db.Categories.Add(Category.Create(categoryId, tenantId, null, "APPAREL", "Apparel", "apparel", null, 0, CategoryConstants.ActiveStatus, null, Now));
        db.Products.Add(Product.Create(productId, tenantId, "P-1", "Product 1", "p-1", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        db.ProductCategories.Add(ProductCategory.Create(mappingId, tenantId, productId, categoryId, true, 0, null, Now));
        await db.SaveChangesAsync();

        var mapping = await db.ProductCategories.AsNoTracking().SingleAsync(x => x.Id == mappingId);
        Assert.Equal(categoryId, mapping.CategoryId);
        Assert.Equal(productId, mapping.ProductId);
        Assert.True(await db.Categories.AsNoTracking().AnyAsync(x => x.Id == categoryId));
    }

    private static async Task SeedTenantsAsync(EPosDbContext db, params Guid[] tenantIds)
    {
        if (!await db.Currencies.AnyAsync(x => x.CurrencyCode == "LKR"))
        {
            db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
        }

        foreach (var tenantId in tenantIds)
        {
            var suffix = tenantId.ToString("N")[..8];
            db.Tenants.Add(Tenant.Create(
                tenantId,
                $"CAT-{suffix}",
                $"cat-{suffix}",
                $"Category Tenant {suffix}",
                TenantStatusConstants.Active,
                "LKR",
                "UTC",
                null,
                null,
                Now));
        }

        await db.SaveChangesAsync();
    }

    private static async Task CreateLegacyCategoriesTableAsync(EPosDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE categories (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                department_id uuid NOT NULL,
                parent_category_id uuid NULL,
                category_code varchar(80) NOT NULL,
                category_name varchar(200) NOT NULL,
                status varchar(40) NOT NULL
            );
            """);
    }

    private static Task InsertLegacyCategoryAsync(
        EPosDbContext db,
        Guid id,
        Guid tenantId,
        Guid departmentId,
        string code,
        string name,
        Guid? parentCategoryId = null) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO categories (id, tenant_id, department_id, parent_category_id, category_code, category_name, status)
            VALUES ({id}, {tenantId}, {departmentId}, {parentCategoryId}, {code}, {name}, 'ACTIVE');
            """);

    private static Task<int> CountLegacyRowsAsync(EPosDbContext db) =>
        db.Database.SqlQueryRaw<int>("""SELECT COUNT(*)::int AS "Value" FROM categories""").SingleAsync();

    private static Task<Guid> GetLegacyIdAsync(EPosDbContext db, string code, Guid departmentId) =>
        db.Database.SqlQueryRaw<Guid>(
                """
                SELECT id AS "Value"
                FROM categories
                WHERE category_code = {0} AND department_id = {1}
                """,
                code,
                departmentId)
            .SingleAsync();

    private static Task<Guid> GetLegacyDepartmentAsync(EPosDbContext db, string code) =>
        db.Database.SqlQueryRaw<Guid>(
                """
                SELECT department_id AS "Value"
                FROM categories
                WHERE category_code = {0}
                """,
                code)
            .SingleAsync();

    private static Task<Guid> GetLegacyParentAsync(EPosDbContext db, Guid id) =>
        db.Database.SqlQueryRaw<Guid>(
                """
                SELECT parent_category_id AS "Value"
                FROM categories
                WHERE id = {0}
                """,
                id)
            .SingleAsync();

    private static EPosDbContext CreateDb(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static async Task<string?> TryConnectAsync()
    {
        foreach (var candidate in CandidateConnectionStrings)
        {
            try
            {
                await using var connection = new NpgsqlConnection(candidate);
                await connection.OpenAsync();
                return candidate;
            }
            catch
            {
                // try next candidate
            }
        }

        return null;
    }

    private sealed class DisposablePostgresHarness : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _adminConnectionString;

        private DisposablePostgresHarness(string databaseName, string connectionString, string adminConnectionString)
        {
            _databaseName = databaseName;
            ConnectionString = connectionString;
            _adminConnectionString = adminConnectionString;
        }

        public string ConnectionString { get; }

        public static async Task<DisposablePostgresHarness> CreateAsync(string baseConnectionString, string prefix, bool ensureCreated = true)
        {
            var databaseName = $"{prefix}_{Guid.NewGuid():N}";
            var adminConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            await using (var admin = new NpgsqlConnection(adminConnectionString))
            {
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
                await create.ExecuteNonQueryAsync();
            }

            var connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Database = databaseName,
                IncludeErrorDetail = true
            }.ConnectionString;

            if (ensureCreated)
            {
                await using var db = CreateDb(connectionString);
                await db.Database.EnsureCreatedAsync();
            }

            return new DisposablePostgresHarness(databaseName, connectionString, adminConnectionString);
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
                             admin))
            {
                terminate.Parameters.AddWithValue("database", _databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
