using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class RetailBusinessCodeMigrationTests
{
    private const string OriginalMigration = "20260804190000_BackfillDevelopmentRetailBusinessCode";
    private const string CorrectiveMigration = "20260805120000_ApplyProductionSafeRetailBusinessCodeRepair";
    private const string PreviousMigration = "20260804110736_AddFlow4ManualPaymentRuntime";
    private const string SeedDescription = "Development retail tenant seed business type.";
    private static readonly DateTimeOffset OriginalUpdatedAt = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GuardedRepair_UpdatesOnlyOneEligibleLegacySeed()
    {
        await WithScratchDatabaseAsync(async connectionString =>
        {
            await CreateBusinessTypesTableAsync(connectionString);
            var candidate = Guid.NewGuid();
            var unrelated = Guid.NewGuid();
            await InsertBusinessTypeAsync(connectionString, candidate, string.Empty, "Retail", SeedDescription);
            await InsertBusinessTypeAsync(connectionString, unrelated, "WHOLESALE", "Wholesale", "Production catalogue row.");

            await ExecuteRepairAsync(connectionString);

            Assert.Equal("RETAIL", await GetCodeAsync(connectionString, candidate));
            Assert.Equal("WHOLESALE", await GetCodeAsync(connectionString, unrelated));
        });
    }

    [Fact]
    public async Task GuardedRepair_LeavesExistingCorrectAndDifferentCodesUnchanged()
    {
        await WithScratchDatabaseAsync(async connectionString =>
        {
            await CreateBusinessTypesTableAsync(connectionString);
            var correct = Guid.NewGuid();
            var different = Guid.NewGuid();
            await InsertBusinessTypeAsync(connectionString, correct, "RETAIL", "Retail", SeedDescription);
            await InsertBusinessTypeAsync(connectionString, different, "HOSPITALITY", "Retail", SeedDescription);

            await ExecuteRepairAsync(connectionString);

            Assert.Equal(("RETAIL", OriginalUpdatedAt.UtcDateTime), await GetStateAsync(connectionString, correct));
            Assert.Equal(("HOSPITALITY", OriginalUpdatedAt.UtcDateTime), await GetStateAsync(connectionString, different));
        });
    }

    [Fact]
    public async Task GuardedRepair_RetailCollisionFailsBeforeChangingCandidate()
    {
        await WithScratchDatabaseAsync(async connectionString =>
        {
            await CreateBusinessTypesTableAsync(connectionString);
            var candidate = Guid.NewGuid();
            await InsertBusinessTypeAsync(connectionString, candidate, string.Empty, "Retail", SeedDescription);
            await InsertBusinessTypeAsync(connectionString, Guid.NewGuid(), "RETAIL", "Retail Operations", null);

            var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteRepairAsync(connectionString));

            Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
            Assert.Equal(string.Empty, await GetCodeAsync(connectionString, candidate));
        });
    }

    [Fact]
    public async Task GuardedRepair_MissingDevelopmentSeedIsSafeNoOp()
    {
        await WithScratchDatabaseAsync(async connectionString =>
        {
            await CreateBusinessTypesTableAsync(connectionString);
            var productionRow = Guid.NewGuid();
            await InsertBusinessTypeAsync(connectionString, productionRow, "SERVICES", "Services", null);

            await ExecuteRepairAsync(connectionString);

            Assert.Equal(("SERVICES", OriginalUpdatedAt.UtcDateTime), await GetStateAsync(connectionString, productionRow));
        });
    }

    [Fact]
    public async Task GuardedRepair_AmbiguousCandidatesFailWithoutPartialUpdate()
    {
        await WithScratchDatabaseAsync(async connectionString =>
        {
            await CreateBusinessTypesTableAsync(connectionString);
            await ExecuteAsync(connectionString, "DROP INDEX ix_business_types_business_code;");
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            await InsertBusinessTypeAsync(connectionString, first, string.Empty, "Retail", SeedDescription);
            await InsertBusinessTypeAsync(connectionString, second, "   ", " retail ", SeedDescription);

            var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteRepairAsync(connectionString));

            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal(string.Empty, await GetCodeAsync(connectionString, first));
            Assert.Equal("   ", await GetCodeAsync(connectionString, second));
        });
    }

    [Fact]
    public async Task GuardedRepair_IsIdempotentAndRollbackPolicyIsNonDestructive()
    {
        await WithScratchDatabaseAsync(async connectionString =>
        {
            await CreateBusinessTypesTableAsync(connectionString);
            var candidate = Guid.NewGuid();
            await InsertBusinessTypeAsync(connectionString, candidate, string.Empty, "Retail", SeedDescription);

            await ExecuteRepairAsync(connectionString);
            var firstState = await GetStateAsync(connectionString, candidate);
            await ExecuteRepairAsync(connectionString);

            Assert.Equal(firstState, await GetStateAsync(connectionString, candidate));
            Assert.Equal("RETAIL", firstState.Code);
        });
    }

    [Fact]
    public async Task FullMigrationChain_AppliesCleanlyAndCreatesValidRetailCode()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await using var db = CreateDb(connectionString);
            await db.Database.MigrateAsync();

            Assert.Equal("RETAIL", await db.BusinessTypes
                .Where(x => x.BusinessName == "Retail" && x.Description == SeedDescription)
                .Select(x => x.BusinessCode)
                .SingleAsync());
            Assert.NotNull(await new PlatformTenantRepository(db)
                .GetActiveBusinessTypeIdByCodeAsync("RETAIL", default));
            Assert.Contains(CorrectiveMigration, await db.Database.GetAppliedMigrationsAsync());
        });
    }

    [Fact]
    public async Task ExistingOriginalMigrationHistory_AppliesForwardCorrectionSafely()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await using var db = CreateDb(connectionString);
            await db.Database.MigrateAsync(PreviousMigration);
            await ExecuteAsync(connectionString, """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ('20260804190000_BackfillDevelopmentRetailBusinessCode', '10.0.0');
                """);

            await db.Database.MigrateAsync();

            Assert.Equal("RETAIL", await db.BusinessTypes
                .Where(x => x.BusinessName == "Retail" && x.Description == SeedDescription)
                .Select(x => x.BusinessCode)
                .SingleAsync());
            Assert.Contains(CorrectiveMigration, await db.Database.GetAppliedMigrationsAsync());
        });
    }

    [Fact]
    public async Task ApplyRollbackReapply_PreservesLegitimateRetailCode()
    {
        await WithDatabaseAsync(async connectionString =>
        {
            await using var db = CreateDb(connectionString);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync(PreviousMigration);

            Assert.Equal("RETAIL", await GetDevelopmentRetailCodeAsync(db));
            Assert.DoesNotContain(OriginalMigration, await db.Database.GetAppliedMigrationsAsync());

            await db.Database.MigrateAsync();

            Assert.Equal("RETAIL", await GetDevelopmentRetailCodeAsync(db));
            Assert.Contains(CorrectiveMigration, await db.Database.GetAppliedMigrationsAsync());
        });
    }

    private static async Task WithScratchDatabaseAsync(Func<string, Task> test) =>
        await WithDatabaseAsync(test);

    private static async Task WithDatabaseAsync(Func<string, Task> test)
    {
        var adminConnectionString = Environment.GetEnvironmentVariable("FLOW4_RETAIL_MIGRATION_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(adminConnectionString))
        {
            return;
        }

        var databaseName = $"flow4_retail_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName,
            IncludeErrorDetail = true
        }.ConnectionString;

        try
        {
            await test(connectionString);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
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

    private static async Task CreateBusinessTypesTableAsync(string connectionString)
    {
        await ExecuteAsync(connectionString, """
            CREATE TABLE business_types (
                id uuid PRIMARY KEY,
                business_code varchar(80) NOT NULL,
                business_name varchar(150) NOT NULL,
                description text NULL,
                status varchar(40) NOT NULL,
                updated_at timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX ix_business_types_business_code ON business_types (business_code);
            """);
    }

    private static async Task InsertBusinessTypeAsync(
        string connectionString,
        Guid id,
        string code,
        string name,
        string? description)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO business_types (id, business_code, business_name, description, status, updated_at)
            VALUES (@id, @code, @name, @description, 'ACTIVE', @updatedAt);
            """, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("description", (object?)description ?? DBNull.Value);
        command.Parameters.AddWithValue("updatedAt", OriginalUpdatedAt);
        await command.ExecuteNonQueryAsync();
    }

    private static Task ExecuteRepairAsync(string connectionString) =>
        ExecuteAsync(connectionString, RetailBusinessCodeRepairSql.Up);

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> GetCodeAsync(string connectionString, Guid id) =>
        (await GetStateAsync(connectionString, id)).Code;

    private static async Task<(string Code, DateTime UpdatedAt)> GetStateAsync(string connectionString, Guid id)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT business_code, updated_at FROM business_types WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetDateTime(1).ToUniversalTime());
    }

    private static EPosDbContext CreateDb(string connectionString) => new(
        new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static Task<string> GetDevelopmentRetailCodeAsync(EPosDbContext db) =>
        db.BusinessTypes
            .Where(x => x.BusinessName == "Retail" && x.Description == SeedDescription)
            .Select(x => x.BusinessCode)
            .SingleAsync();
}
