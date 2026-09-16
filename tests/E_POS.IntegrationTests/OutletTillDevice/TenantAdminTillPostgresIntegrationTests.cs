using System;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Application.Common.Contracts;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class TenantAdminTillPostgresIntegrationTests
{
    [Fact]
    public async Task ListAsync_TranslatesSuccessfully_InPostgres()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin")
            .Options;

        await using var dbContext = new EPosDbContext(options);
        var repository = new TenantAdminTillRepository(
            dbContext, 
            new FakeTillMonitoringOptionsSnapshot(new TillMonitoringOptions { HeartbeatTimeoutSeconds = 300 }),
            new FakeDateTimeProvider(DateTimeOffset.UtcNow));

        // This should throw if EF Core translation fails
        var ex = await Record.ExceptionAsync(() => repository.ListAsync(
            Guid.NewGuid(), 
            null, 
            null, 
            null, 
            1, 
            10, 
            "name", 
            "asc", 
            CancellationToken.None));

        if (ex != null)
        {
            Console.WriteLine("EXCEPTION TYPE: " + ex.GetType().Name);
            Console.WriteLine("EXCEPTION MESSAGE: " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("INNER EXCEPTION: " + ex.InnerException.Message);
            }
        }

        Assert.Null(ex);
    }

    private sealed class FakeTillMonitoringOptionsSnapshot : Microsoft.Extensions.Options.IOptionsSnapshot<TillMonitoringOptions>
    {
        public FakeTillMonitoringOptionsSnapshot(TillMonitoringOptions value)
        {
            Value = value;
        }
        public TillMonitoringOptions Value { get; }
        public TillMonitoringOptions Get(string? name) => Value;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset now)
        {
            UtcNow = now;
        }
        public DateTimeOffset UtcNow { get; }
    }
}
