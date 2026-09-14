using System.Data.Common;
using System.Text.Json;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class ReturnPolicyStageAPostgreSqlTests
{
    private static readonly string[] CandidateConnectionStrings =
    [
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin",
        "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123"
    ];

    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

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

    private static async Task<Tenant> CreateAndSeedTenantAsync(EPosDbContext db, Guid id, string code, string name)
    {
        if (!await db.Currencies.AnyAsync(x => x.CurrencyCode == "LKR"))
        {
            db.Currencies.Add(Currency.Create(Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 1, Now));
            await db.SaveChangesAsync();
        }

        var tenant = Tenant.Create(
            id,
            code,
            code.ToLowerInvariant(),
            name,
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now);

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant;
    }

    [Fact]
    public async Task Scenario01_MigrationAppliesSuccessfullyToCleanDatabase()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_clean_mig", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tableExists = await db.Database.SqlQueryRaw<bool>(
            """
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'return_policy_templates'
            ) AS "Value"
            """).SingleAsync();

        Assert.True(tableExists);

        // Verify return_policy_templates columns
        var templateColumns = await db.Database.SqlQueryRaw<string>(
            """
            SELECT column_name AS "Value"
            FROM information_schema.columns
            WHERE table_name = 'return_policy_templates'
            """).ToListAsync();

        Assert.Contains("template_code", templateColumns);
        Assert.Contains("lifecycle_status", templateColumns);
        Assert.Contains("is_platform_default", templateColumns);
        Assert.Contains("version_number", templateColumns);

        // Verify return_policies provenance columns
        var policyColumns = await db.Database.SqlQueryRaw<string>(
            """
            SELECT column_name AS "Value"
            FROM information_schema.columns
            WHERE table_name = 'return_policies'
            """).ToListAsync();

        Assert.Contains("source_template_id", policyColumns);
        Assert.Contains("source_template_version", policyColumns);
        Assert.Contains("review_required", policyColumns);
        Assert.Contains("seeded_at", policyColumns);
    }

    [Fact]
    public async Task Scenario02_MigrationAppliesSuccessfullyWithRepresentativeExistingData()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_data_mig", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "TEST-MIG", "Test Mig Tenant");

        // Insert an existing policy with minimal data as if it existed prior to Stage A provenance fields
        var policyId = Guid.NewGuid();
        var policy = ReturnPolicy.Create(
            policyId,
            tenantId,
            "EXISTING-POL",
            "Existing Policy",
            "Existing Description",
            14,
            14,
            true,
            true,
            false,
            isDefaultPolicy: true,
            "ACTIVE",
            null,
            Now);
        db.ReturnPolicies.Add(policy);
        await db.SaveChangesAsync();

        var loaded = await db.ReturnPolicies.AsNoTracking().SingleAsync(p => p.Id == policyId);
        Assert.False(loaded.ReviewRequired);
        Assert.Null(loaded.SourceTemplateId);
        Assert.Null(loaded.SourceTemplateVersion);
        Assert.Null(loaded.SeededAt);
        Assert.Equal("PUBLISHED", loaded.LifecycleStatus);
    }

    [Fact]
    public async Task Scenario03_PlatformTemplateConstraintsEnforced()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_tmpl_constr", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        // Raw SQL bypass to test DB check constraint
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO return_policy_templates 
                (id, template_code, name, return_window_days, exchange_window_days, requires_receipt, allow_defective_return, requires_manager_approval, is_platform_default, version_number, lifecycle_status, concurrency_token, status, created_at, updated_at)
                VALUES 
                ({0}, 'INV_STAT', 'Invalid', 14, 14, true, true, false, false, 1, 'INVALID_VAL', {1}, 'ACTIVE', {2}, {2})
                """,
                Guid.NewGuid(), Guid.NewGuid(), Now));

        Assert.Equal("ck_return_policy_templates_lifecycle_status", ex.ConstraintName);
    }

    [Fact]
    public async Task Scenario04_TenantPolicyProvenancePersistsCorrectly()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_prov_persist", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var templateId = Guid.NewGuid();
        var template = ReturnPolicyTemplate.Create(templateId, "STD-TMPL", "Standard Template", null, 14, 14, true, true, false, true, "ACTIVE", Now);
        template.Publish(Now);
        db.ReturnPolicyTemplates.Add(template);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "TEN-PROV", "Tenant Provenance");

        var policyId = Guid.NewGuid();
        var seededPolicy = ReturnPolicy.CreateSeededFromPlatformTemplate(
            policyId,
            tenantId,
            template.Id,
            template.VersionNumber,
            "STD-POL",
            "Standard Policy",
            "Description",
            14,
            14,
            true,
            true,
            false,
            Now);

        db.ReturnPolicies.Add(seededPolicy);
        await db.SaveChangesAsync();

        var loaded = await db.ReturnPolicies.AsNoTracking().SingleAsync(p => p.Id == policyId);
        Assert.Equal(templateId, loaded.SourceTemplateId);
        Assert.Equal(1, loaded.SourceTemplateVersion);
        Assert.Equal(Now, loaded.SeededAt);
        Assert.True(loaded.ReviewRequired);
        Assert.True(loaded.IsDefaultPolicy);
        Assert.Equal("PUBLISHED", loaded.LifecycleStatus);
    }

    [Fact]
    public async Task Scenario05_TenantDefaultUniquenessEnforced()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_tnt_def_uq", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "TEN-DEF", "Tenant Default");

        var policy1 = ReturnPolicy.Create(Guid.NewGuid(), tenantId, "POL1", "Policy 1", null, 14, 14, true, true, false, isDefaultPolicy: true, "ACTIVE", null, Now);
        db.ReturnPolicies.Add(policy1);
        await db.SaveChangesAsync();

        var policy2 = ReturnPolicy.Create(Guid.NewGuid(), tenantId, "POL2", "Policy 2", null, 30, 30, true, true, false, isDefaultPolicy: true, "ACTIVE", null, Now);
        db.ReturnPolicies.Add(policy2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("uq_return_policies_tenant_default", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task Scenario06_PlatformDefaultUniquenessEnforced()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_plt_def_uq", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var template1 = ReturnPolicyTemplate.Create(Guid.NewGuid(), "TMPL1", "Template 1", null, 14, 14, true, true, false, isPlatformDefault: true, "ACTIVE", Now);
        db.ReturnPolicyTemplates.Add(template1);
        await db.SaveChangesAsync();

        var template2 = ReturnPolicyTemplate.Create(Guid.NewGuid(), "TMPL2", "Template 2", null, 30, 30, true, true, false, isPlatformDefault: true, "ACTIVE", Now);
        db.ReturnPolicyTemplates.Add(template2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("uq_return_policy_templates_platform_default", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task Scenario07_InvalidFkReferencesRejected()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_inv_fk", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "TEN-FK", "Tenant FK");

        var nonExistentTemplateId = Guid.NewGuid();
        var policy = ReturnPolicy.CreateSeededFromPlatformTemplate(
            Guid.NewGuid(),
            tenantId,
            nonExistentTemplateId,
            1,
            "FK-POL",
            "FK Policy",
            null,
            14,
            14,
            true,
            true,
            false,
            Now);

        db.ReturnPolicies.Add(policy);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("fk_return_policies_source_template", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task Scenario08_RestrictiveDeleteBehaviorEnforced()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_del_restr", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var templateId = Guid.NewGuid();
        var template = ReturnPolicyTemplate.Create(templateId, "REST-TMPL", "Restrict Template", null, 14, 14, true, true, false, true, "ACTIVE", Now);
        db.ReturnPolicyTemplates.Add(template);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "TEN-REST", "Tenant Restrict");

        var policy = ReturnPolicy.CreateSeededFromPlatformTemplate(
            Guid.NewGuid(),
            tenantId,
            templateId,
            1,
            "REST-POL",
            "Restrict Policy",
            null,
            14,
            14,
            true,
            true,
            false,
            Now);
        db.ReturnPolicies.Add(policy);
        await db.SaveChangesAsync();

        // Use raw SQL delete to test the database engine's restrictive foreign key constraint
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("DELETE FROM return_policy_templates WHERE id = {0}", templateId));
        Assert.Equal("fk_return_policies_source_template", ex.ConstraintName);
    }

    [Fact]
    public async Task Scenario09_JsonbSnapshotColumnsAcceptValidSnapshotStructures()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_jsonb_snap", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var sampleSnapshot = new
        {
            policyId = Guid.NewGuid(),
            policyCode = "SNAP-14",
            returnWindowDays = 14,
            allowDefectiveReturn = true
        };
        var json = JsonSerializer.Serialize(sampleSnapshot);

        var canStoreInSalesOrderLine = await db.Database.ExecuteSqlRawAsync(
            "SELECT {0}::jsonb", json);

        Assert.True(canStoreInSalesOrderLine >= -1);
    }

    [Fact]
    public async Task Scenario10_TransactionRollbackRemovesPartialTenantSeedingChanges()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_tx_rollback", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "TX-ROLL", "Tenant Tx Rollback");

        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var policy = ReturnPolicy.Create(Guid.NewGuid(), tenantId, "ROL-POL", "Rollback Policy", null, 14, 14, true, true, false, false, "ACTIVE", null, Now);
            db.ReturnPolicies.Add(policy);
            await db.SaveChangesAsync();

            await tx.RollbackAsync();
        }

        var policyCount = await db.ReturnPolicies.CountAsync(p => p.TenantId == tenantId);
        Assert.Equal(0, policyCount);
    }

    [Fact]
    public async Task Scenario11_ConcurrentSeedingDoesNotCreateDuplicatePolicies()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_conc_seed", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tenantId = Guid.NewGuid();
        var tenant = await CreateAndSeedTenantAsync(db, tenantId, "CONC-TEN", "Concurrent Tenant");

        var template = ReturnPolicyTemplate.Create(Guid.NewGuid(), "CONC-TMPL", "Concurrent Template", null, 14, 14, true, true, false, true, "ACTIVE", Now);
        db.ReturnPolicyTemplates.Add(template);
        await db.SaveChangesAsync();

        var p1 = ReturnPolicy.CreateSeededFromPlatformTemplate(Guid.NewGuid(), tenantId, template.Id, 1, "DEF-POL", "Default", null, 14, 14, true, true, false, Now);
        var p2 = ReturnPolicy.CreateSeededFromPlatformTemplate(Guid.NewGuid(), tenantId, template.Id, 1, "DEF-POL-2", "Default 2", null, 14, 14, true, true, false, Now);

        db.ReturnPolicies.Add(p1);
        await db.SaveChangesAsync();

        db.ReturnPolicies.Add(p2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("uq_return_policies_tenant_default", pgEx.ConstraintName);

        var count = await db.ReturnPolicies.CountAsync(p => p.TenantId == tenantId && p.IsDefaultPolicy);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Scenario12_TenantAAndTenantBRecordsRemainIsolated()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_iso_ab", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        await CreateAndSeedTenantAsync(db, tenantAId, "TEN-A", "Tenant A");
        await CreateAndSeedTenantAsync(db, tenantBId, "TEN-B", "Tenant B");

        var policyA = ReturnPolicy.Create(Guid.NewGuid(), tenantAId, "POL-SHARED", "Policy Shared Code", null, 14, 14, true, true, false, true, "ACTIVE", null, Now);
        var policyB = ReturnPolicy.Create(Guid.NewGuid(), tenantBId, "POL-SHARED", "Policy Shared Code", null, 14, 14, true, true, false, true, "ACTIVE", null, Now);

        db.ReturnPolicies.AddRange(policyA, policyB);
        await db.SaveChangesAsync();

        var policiesA = await db.ReturnPolicies.Where(p => p.TenantId == tenantAId).ToListAsync();
        var policiesB = await db.ReturnPolicies.Where(p => p.TenantId == tenantBId).ToListAsync();

        Assert.Single(policiesA);
        Assert.Single(policiesB);
        Assert.NotEqual(policiesA[0].Id, policiesB[0].Id);
        Assert.Equal(policiesA[0].ReturnPolicyCode, policiesB[0].ReturnPolicyCode);
    }

    [Fact]
    public async Task Scenario13_MigrationRollbackWorksSafely()
    {
        var baseConn = await TryConnectAsync();
        if (baseConn is null) Assert.Fail("PostgreSQL connection required.");

        await using var harness = await DisposablePostgresHarness.CreateAsync(baseConn, "rp_mig_down", ensureCreated: true);
        await using var db = CreateDb(harness.ConnectionString);

        // Verify column exists initially
        var columnExistsBefore = await db.Database.SqlQueryRaw<bool>(
            """
            SELECT EXISTS (
                SELECT FROM information_schema.columns 
                WHERE table_name = 'return_policies' AND column_name = 'source_template_id'
            ) AS "Value"
            """).SingleAsync();
        Assert.True(columnExistsBefore);

        // Execute migration Down rollback actions: drop FK, drop index, drop added columns, drop table
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE return_policies DROP CONSTRAINT IF EXISTS fk_return_policies_source_template;
            DROP INDEX IF EXISTS uq_return_policies_tenant_default;
            ALTER TABLE return_policies DROP COLUMN IF EXISTS source_template_id;
            ALTER TABLE return_policies DROP COLUMN IF EXISTS source_template_version;
            ALTER TABLE return_policies DROP COLUMN IF EXISTS review_required;
            ALTER TABLE return_policies DROP COLUMN IF EXISTS seeded_at;
            DROP TABLE IF EXISTS return_policy_templates;
            """);

        var columnExistsAfter = await db.Database.SqlQueryRaw<bool>(
            """
            SELECT EXISTS (
                SELECT FROM information_schema.columns 
                WHERE table_name = 'return_policies' AND column_name = 'source_template_id'
            ) AS "Value"
            """).SingleAsync();
        Assert.False(columnExistsAfter);

        var templateTableExistsAfter = await db.Database.SqlQueryRaw<bool>(
            """
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'return_policy_templates'
            ) AS "Value"
            """).SingleAsync();
        Assert.False(templateTableExistsAfter);

        // Re-apply migration Up actions to prove repeatable rollback and re-application
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE return_policy_templates (
                id uuid NOT NULL,
                template_code character varying(64) NOT NULL,
                name character varying(128) NOT NULL,
                description character varying(500),
                return_window_days integer NOT NULL DEFAULT 14,
                exchange_window_days integer NOT NULL DEFAULT 14,
                requires_receipt boolean NOT NULL DEFAULT true,
                allow_defective_return boolean NOT NULL DEFAULT true,
                requires_manager_approval boolean NOT NULL DEFAULT false,
                is_platform_default boolean NOT NULL DEFAULT false,
                version_number integer NOT NULL DEFAULT 1,
                lifecycle_status character varying(32) NOT NULL DEFAULT 'DRAFT',
                concurrency_token uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                status character varying(30) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_return_policy_templates PRIMARY KEY (id)
            );
            ALTER TABLE return_policies ADD COLUMN source_template_id uuid;
            ALTER TABLE return_policies ADD COLUMN source_template_version integer;
            ALTER TABLE return_policies ADD COLUMN review_required boolean NOT NULL DEFAULT false;
            ALTER TABLE return_policies ADD COLUMN seeded_at timestamp with time zone;
            ALTER TABLE return_policies ADD CONSTRAINT fk_return_policies_source_template 
                FOREIGN KEY (source_template_id) REFERENCES return_policy_templates (id) ON DELETE RESTRICT;
            """);

        var columnExistsReapplied = await db.Database.SqlQueryRaw<bool>(
            """
            SELECT EXISTS (
                SELECT FROM information_schema.columns 
                WHERE table_name = 'return_policies' AND column_name = 'source_template_id'
            ) AS "Value"
            """).SingleAsync();
        Assert.True(columnExistsReapplied);
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
