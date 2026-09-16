using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

/// <summary>Keep ninety days of transport transitions and each device's newest observation. Physical tests and audit/financial history are never removed here.</summary>
public sealed class HardwareTelemetryRetentionWorker(IServiceScopeFactory scopes,
    ILogger<HardwareTelemetryRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
                var count = await PruneAsync(db, stoppingToken);
                logger.LogInformation("Hardware telemetry retention completed. RemovedTransitions={Count}", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { logger.LogWarning("Hardware telemetry retention failed; retry scheduled without exposing database details."); }
        }
    }

    public static Task<int> PruneAsync(EPosDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("""
        DELETE FROM hardware_test_logs old WHERE old.id IN (
          SELECT h.id FROM hardware_test_logs h WHERE h.test_type='TELEMETRY'
            AND h.tested_at < now() - interval '90 days'
            AND EXISTS (SELECT 1 FROM hardware_test_logs newer WHERE newer.tenant_id=h.tenant_id
              AND newer.hardware_device_id=h.hardware_device_id AND newer.test_type='TELEMETRY'
              AND newer.tested_at > h.tested_at)
          ORDER BY h.tested_at LIMIT 10000
        )
        """, ct);
}
