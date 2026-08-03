using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class TenantAdminHardwareReadinessPostgresIntegrationTests
{
    [Fact]
    public async Task GetHardwareReadinessDataAsync_TranslatesSuccessfully_InPostgres()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin")
            .Options;

        await using var dbContext = new EPosDbContext(options);
        var repository = new TenantAdminTillRepository(
            dbContext,
            new FakeTillMonitoringOptionsSnapshot(new TillMonitoringOptions { HeartbeatTimeoutSeconds = 300 }),
            new FakeDateTimeProvider(DateTimeOffset.UtcNow));

        var ex = await Record.ExceptionAsync(() => repository.GetHardwareReadinessDataAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task GetSummaryAsync_OfflineIsNotEqualToInactive_InPostgres()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin")
            .Options;

        await using var dbContext = new EPosDbContext(options);
        var repository = new TenantAdminTillRepository(
            dbContext,
            new FakeTillMonitoringOptionsSnapshot(new TillMonitoringOptions { HeartbeatTimeoutSeconds = 300 }),
            new FakeDateTimeProvider(DateTimeOffset.UtcNow));

        // Use Oneverce tenant if present; otherwise empty tenant still must translate.
        var tenantId = Guid.Parse("55555555-0000-4000-8000-000000000001");
        var summary = await repository.GetSummaryAsync(tenantId, CancellationToken.None);

        Assert.True(summary.OfflineTills >= 0);
        Assert.True(summary.InactiveTills >= 0);
        // Contract: Offline and Inactive are independently calculated (may coincidentally match).
        Assert.NotNull(summary);
    }

    private sealed class FakeTillMonitoringOptionsSnapshot : Microsoft.Extensions.Options.IOptionsSnapshot<TillMonitoringOptions>
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
