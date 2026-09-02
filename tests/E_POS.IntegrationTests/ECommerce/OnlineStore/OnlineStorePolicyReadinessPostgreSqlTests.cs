using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.OnlineStore;

public sealed class OnlineStorePolicyReadinessPostgreSqlTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task PostgreSqlPolicyProjection_CountsOnlyCurrentTenantChannelPublishedRequiredTypes()
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
            CREATE TEMP TABLE online_store_policy_readiness_test (
                tenant_id uuid NOT NULL,
                sales_channel_id uuid NOT NULL,
                policy_type text NOT NULL,
                version text NOT NULL,
                status text NOT NULL
            );
            CREATE UNIQUE INDEX ux_online_store_policy_current_published_test
                ON online_store_policy_readiness_test(tenant_id, sales_channel_id, policy_type)
                WHERE status = 'PUBLISHED';
            INSERT INTO online_store_policy_readiness_test VALUES
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'TERMS', '1.0', 'PUBLISHED'),
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'PRIVACY', '1.0', 'PUBLISHED'),
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'CANCELLATION', '1.0', 'PUBLISHED'),
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'COLLECTION', '1.0', 'DRAFT'),
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'COLLECTION', '0.9', 'ARCHIVED'),
                ('33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222', 'COLLECTION', '1.0', 'PUBLISHED'),
                ('11111111-1111-1111-1111-111111111111', '44444444-4444-4444-4444-444444444444', 'COLLECTION', '1.0', 'PUBLISHED'),
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'RETURN_REFUND', '1.0', 'PUBLISHED');
            """, connection, transaction))
        {
            await setup.ExecuteNonQueryAsync();
        }

        await using var countCommand = new NpgsqlCommand("""
            SELECT COUNT(DISTINCT policy_type)
            FROM online_store_policy_readiness_test
            WHERE tenant_id = '11111111-1111-1111-1111-111111111111'
              AND sales_channel_id = '22222222-2222-2222-2222-222222222222'
              AND status = 'PUBLISHED'
              AND policy_type = ANY (ARRAY['TERMS', 'PRIVACY', 'CANCELLATION', 'COLLECTION']);
            """, connection, transaction);

        Assert.Equal(3L, (long)(await countCommand.ExecuteScalarAsync())!);

        await using (var publishMissing = new NpgsqlCommand("""
            INSERT INTO online_store_policy_readiness_test VALUES
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'COLLECTION', '1.0', 'PUBLISHED');
            """, connection, transaction))
        {
            await publishMissing.ExecuteNonQueryAsync();
        }

        Assert.Equal(4L, (long)(await countCommand.ExecuteScalarAsync())!);

        await using var duplicatePublished = new NpgsqlCommand("""
            INSERT INTO online_store_policy_readiness_test VALUES
                ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'TERMS', '2.0', 'PUBLISHED');
            """, connection, transaction);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => duplicatePublished.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);

        await transaction.RollbackAsync();
    }
}
