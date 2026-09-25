using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed partial class PosCheckoutRepositoryTests
{
    [CashPostgreSqlFact]
    public async Task ReconciliationFence_OnPostgreSql_BlocksOriginalAlreadyWaitingForLock()
    {
        var supplied = Environment.GetEnvironmentVariable("POS_CASH_TEST_CONNECTION")!;
        var config = new NpgsqlConnectionStringBuilder(supplied);
        Assert.Contains(config.Host, new[] { "localhost", "127.0.0.1" });
        var database = $"pos_cash_gate_{Guid.NewGuid():N}";
        var adminConfig = new NpgsqlConnectionStringBuilder(supplied) { Database = "postgres", Pooling = false };
        await using var admin = new NpgsqlConnection(adminConfig.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            config.Database = database;
            config.Pooling = false;
            await using (var schema = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>()
                .UseNpgsql(config.ConnectionString).Options))
                await schema.Database.EnsureCreatedAsync();
            await using var owner = new NpgsqlConnection(config.ConnectionString);
            await owner.OpenAsync();
            await using var ownerDb = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(owner).Options);
            var tenant = Guid.NewGuid();
            var user = Guid.NewGuid();
            var key = Guid.NewGuid().ToString("N");
            var scope = $"pos-cash:{tenant:N}:{key}";
            await ownerDb.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_lock(hashtextextended({scope}, 0))");
            await using var originalDb = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>()
                .UseNpgsql(config.ConnectionString).Options);
            var original = CreateRepository(originalDb).StartPaymentAsync(tenant, user,
                [PaymentPermissions.AcceptCash], new PosCheckoutStartPaymentRequestDto(
                    Guid.NewGuid(), "NewSale", null, [new(Guid.NewGuid(), 1)], "cash", 100,
                    IdempotencyKey: key), Now, CancellationToken.None);
            try
            {
                var waiting = false;
                for (var i = 0; i < 100 && !waiting; i++)
                {
                    await using var query = new NpgsqlCommand(
                        "SELECT EXISTS(SELECT 1 FROM pg_locks l JOIN pg_stat_activity a ON a.pid=l.pid WHERE a.datname=@db AND l.locktype='advisory' AND NOT l.granted)", owner);
                    query.Parameters.AddWithValue("db", database);
                    waiting = (bool)(await query.ExecuteScalarAsync())!;
                    if (!waiting) await Task.Delay(20);
                }
                Assert.True(waiting, "Original request must be waiting before reconciliation closes the key.");
                var reconciled = await CreateRepository(ownerDb).ReconcileCashPaymentAsync(tenant, user, key, CancellationToken.None);
                Assert.Equal("not_completed", reconciled.Status);
            }
            finally
            {
                await ownerDb.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_unlock(hashtextextended({scope}, 0))");
            }
            var result = await original.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal("pos_checkout.attempt_closed", result.ErrorCode);
            Assert.Empty(await ownerDb.SalesPayments.ToListAsync());
            Assert.Empty(await ownerDb.SalesOrders.ToListAsync());
            Assert.Single(await ownerDb.IdempotencyRequests.ToListAsync());
        }
        finally
        {
            // Only this generated disposable test database is removed; never the supplied database.
            Assert.Matches("^pos_cash_gate_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class CashPostgreSqlFactAttribute : FactAttribute
    {
        public CashPostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POS_CASH_TEST_CONNECTION")))
                Skip = "Set POS_CASH_TEST_CONNECTION to a local PostgreSQL connection; creates a disposable test database.";
        }
    }
}
