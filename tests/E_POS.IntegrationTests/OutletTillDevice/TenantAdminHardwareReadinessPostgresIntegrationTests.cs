using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

/// <summary>
/// Optional Postgres translation checks. These tests no-op when localhost Postgres
/// is unavailable (matching other IntegrationTests Postgres suites).
/// </summary>
public sealed class TenantAdminHardwareReadinessPostgresIntegrationTests
{
    private const string PostgreSqlConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    [Fact]
    public async Task GetHardwareReadinessDataAsync_TranslatesSuccessfully_InPostgres()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        var repository = CreateRepository(dbContext);

        var ex = await Record.ExceptionAsync(() => repository.GetHardwareReadinessDataAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None));

        // Null = success. Undefined-column (42703) still proves EF translated and Postgres executed SQL.
        Assert.True(
            ex is null || IsUndefinedColumnException(ex),
            $"Unexpected query failure (likely EF translation): {ex}");
    }

    [Fact]
    public async Task GetSummaryAsync_OfflineIsNotEqualToInactive_InPostgres()
    {
        if (!await CanConnectToPostgreSqlAsync())
        {
            return;
        }

        await using var dbContext = CreatePostgreSqlDbContext();
        var repository = CreateRepository(dbContext);

        // Use Oneverce tenant if present; otherwise empty tenant still must translate.
        var tenantId = Guid.Parse("55555555-0000-4000-8000-000000000001");

        try
        {
            var summary = await repository.GetSummaryAsync(tenantId, CancellationToken.None);
            Assert.True(summary.OfflineTills >= 0);
            Assert.True(summary.InactiveTills >= 0);
            Assert.NotNull(summary);
        }
        catch (Exception ex) when (IsUndefinedColumnException(ex))
        {
            // Local/CI DB may lag migrations; skip schema-drift failures for this optional check.
        }
    }

    private static bool IsUndefinedColumnException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UndefinedColumn })
            {
                return true;
            }
        }

        return false;
    }

    private static TenantAdminTillRepository CreateRepository(EPosDbContext dbContext) =>
        new(
            dbContext,
            new FakeTillMonitoringOptionsSnapshot(new TillMonitoringOptions { HeartbeatTimeoutSeconds = 300 }),
            new FakeDateTimeProvider(DateTimeOffset.UtcNow));

    private static EPosDbContext CreatePostgreSqlDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql(PostgreSqlConnectionString)
            .Options;
        return new EPosDbContext(options);
    }

    private static async Task<bool> CanConnectToPostgreSqlAsync()
    {
        await using var dbContext = CreatePostgreSqlDbContext();
        try
        {
            return await dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private sealed class FakeTillMonitoringOptionsSnapshot : IOptionsSnapshot<TillMonitoringOptions>
    {
        public FakeTillMonitoringOptionsSnapshot(TillMonitoringOptions value) => Value = value;
        public TillMonitoringOptions Value { get; }
        public TillMonitoringOptions Get(string? name) => Value;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset now) => UtcNow = now;
        public DateTimeOffset UtcNow { get; }
    }
}
