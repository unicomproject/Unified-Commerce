using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class ProductOptionSeedMigrationPostgreSqlTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
    private const string CorrectiveMigration =
        "20260710142500_SeedReferenceProductOptionsBeforeVariableCatalog";
    private const string VariableCatalogMigration =
        "20260710143000_SeedDevelopmentVariableProductCatalog";

    [Fact]
    public async Task CorrectiveSeed_SupportsCleanAndUpgradePathsWithoutProductOptionOrphans()
    {
        if (!await CanConnectAsync()) return;

        var databaseName = $"product_option_seed_{Guid.NewGuid():N}";
        var adminConnectionString = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = "postgres"
        }.ConnectionString;

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var databaseConnectionString = new NpgsqlConnectionStringBuilder(ConnectionString)
            {
                Database = databaseName,
                IncludeErrorDetail = true
            }.ConnectionString;

            await using var db = new EPosDbContext(
                new DbContextOptionsBuilder<EPosDbContext>()
                    .UseNpgsql(databaseConnectionString)
                    .Options);

            await db.Database.MigrateAsync(VariableCatalogMigration);
            await AssertNoOrphansAsync(databaseConnectionString);

            await db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{CorrectiveMigration}'");
            await db.Database.MigrateAsync(VariableCatalogMigration);

            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains(CorrectiveMigration, applied);
            Assert.Contains(VariableCatalogMigration, applied);
            await AssertNoOrphansAsync(databaseConnectionString);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
                             admin))
            {
                terminate.Parameters.AddWithValue("database", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task AssertNoOrphansAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
              (SELECT COUNT(*) FROM product_options child
               LEFT JOIN products parent ON parent.id = child.product_id
               WHERE parent.id IS NULL),
              (SELECT COUNT(*) FROM product_options child
               LEFT JOIN product_option_templates parent ON parent.id = child.source_option_template_id
               WHERE child.source_option_template_id IS NOT NULL AND parent.id IS NULL),
              (SELECT COUNT(*) FROM product_option_values child
               LEFT JOIN product_options parent ON parent.id = child.product_option_id
               WHERE parent.id IS NULL),
              (SELECT COUNT(*) FROM product_option_values child
               LEFT JOIN product_option_template_values parent ON parent.id = child.source_option_template_value_id
               WHERE child.source_option_template_value_id IS NOT NULL AND parent.id IS NULL),
              (SELECT COUNT(*) FROM product_variant_option_values child
               LEFT JOIN product_variants parent ON parent.id = child.product_variant_id
               WHERE parent.id IS NULL),
              (SELECT COUNT(*) FROM product_variant_option_values child
               LEFT JOIN product_options parent ON parent.id = child.product_option_id
               WHERE parent.id IS NULL),
              (SELECT COUNT(*) FROM product_variant_option_values child
               LEFT JOIN product_option_values parent ON parent.id = child.product_option_value_id
               WHERE parent.id IS NULL)
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        for (var index = 0; index < 7; index++)
        {
            Assert.Equal(0L, reader.GetInt64(index));
        }
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
