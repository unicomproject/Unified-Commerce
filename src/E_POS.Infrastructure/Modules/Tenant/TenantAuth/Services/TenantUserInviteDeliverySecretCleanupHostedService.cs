using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Services;

public sealed class TenantUserInviteDeliverySecretCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromHours(1);
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantUserInviteDeliverySecretCleanupHostedService> _logger;

    public TenantUserInviteDeliverySecretCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TenantUserInviteDeliverySecretCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);
        do
        {
            await RunCleanupPassAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCleanupPassAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cleanup = scope.ServiceProvider.GetRequiredService<ITenantUserInviteDeliverySecretCleanupService>();
            await cleanup.CleanupBatchAsync(BatchSize, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during tenant user invite delivery secret cleanup pass.");
        }
    }
}
