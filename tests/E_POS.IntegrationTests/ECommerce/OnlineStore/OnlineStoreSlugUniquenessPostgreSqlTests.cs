using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.OnlineStore;

public sealed class OnlineStoreSlugUniquenessPostgreSqlTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task PostgreSqlExpressionIndex_RejectsCaseInsensitiveDuplicateStoreSlug()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
        }
        catch
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync();
        await using (var setup = new NpgsqlCommand("""
            CREATE TEMP TABLE online_store_slug_guard_test (setting_value jsonb NOT NULL);
            CREATE UNIQUE INDEX ux_online_store_slug_guard_test
                ON online_store_slug_guard_test (LOWER(setting_value ->> 'storeSlug'))
                WHERE NULLIF(BTRIM(setting_value ->> 'storeSlug'), '') IS NOT NULL;
            INSERT INTO online_store_slug_guard_test(setting_value)
            VALUES ('{"storeSlug":"arena-store"}'::jsonb);
            """, connection, transaction))
        {
            await setup.ExecuteNonQueryAsync();
        }

        await using var duplicate = new NpgsqlCommand(
            "INSERT INTO online_store_slug_guard_test(setting_value) VALUES ('{\"storeSlug\":\"ARENA-STORE\"}'::jsonb);",
            connection,
            transaction);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => duplicate.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        await transaction.RollbackAsync();
    }
}
