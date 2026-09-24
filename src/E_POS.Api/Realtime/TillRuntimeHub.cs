using System.Collections.Concurrent;
using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using E_POS.Application.Common.Security;
using System.Security.Claims;

namespace E_POS.Api.Realtime;

public sealed record HardwareMonitor(string ConnectionId, TenantRequestContext Actor, Guid TillId, ClaimsPrincipal Principal);
public sealed class HardwareMonitorRegistry
{
    private readonly ConcurrentDictionary<string, HardwareMonitor> _items = new();
    public void Add(HardwareMonitor monitor) => _items[monitor.ConnectionId] = monitor;
    public void Remove(string id) => _items.TryRemove(id, out _);
    public IEnumerable<HardwareMonitor> All => _items.Values;
    public IEnumerable<HardwareMonitor> ForTill(Guid tenant, Guid till) =>
        _items.Values.Where(x => x.Actor.TenantId == tenant && x.TillId == till);
}

[Authorize(Policy = "TenantOnly")]
public sealed class TillRuntimeHub(ITenantRequestContextFactory contexts, ITillDiagnosticService service,
    ITillConnectionRegistry registry, HardwareMonitorRegistry monitors, TillDiagnosticDispatcher dispatcher,
    ILogger<TillRuntimeHub> logger) : Hub
{
    public const string Route = "/hubs/till-runtime";
    private TenantRequestContext Actor => contexts.TryCreate(Context.User!, out var actor) ? actor :
        throw new HubException("Tenant authentication required.");

    public async Task RegisterPos(string proof)
    {
        try
        {
            var binding = await service.BindAsync(Actor, proof, Context.ConnectionAborted);
            registry.Register(new(Context.ConnectionId, Actor, binding, service.Now, service.Now, Context.User));
        }
        catch (UnauthorizedAccessException) { throw new HubException("Current native POS binding required."); }
    }

    public async Task<TillRuntimeStatus> WatchTill(Guid tillId)
    {
        var actor = Actor;
        if (!actor.HasPermission("tenant.hardware.view") && !actor.HasPermission("tenant.hardware.manage"))
            throw new HubException("Hardware view permission required.");
        try { await service.AuthorizeAsync(actor, tillId, false, Context.ConnectionAborted); }
        catch (UnauthorizedAccessException) { throw new HubException("Till access denied."); }
        monitors.Add(new(Context.ConnectionId, actor, tillId, Context.User!));
        return await dispatcher.StatusAsync(actor, tillId, Context.ConnectionAborted);
    }

    public async Task Heartbeat()
    {
        var current = registry.Get(Context.ConnectionId);
        if (current == null || !await service.IsCurrentAsync(current, Context.ConnectionAborted))
        { registry.Remove(Context.ConnectionId); throw new HubException("POS binding expired."); }
        registry.Touch(Context.ConnectionId, service.Now);
    }

    public async Task<TillDiagnosticScan> ScanStarted(Guid scanId) => await Submit(scanId, "RUNNING", null);
    public async Task<TillDiagnosticScan> DeviceScanResult(Guid scanId, List<TillDiagnosticResult> results) =>
        await Submit(scanId, "COMPLETED", results);

    private async Task<TillDiagnosticScan> Submit(Guid id, string status, List<TillDiagnosticResult>? results)
    {
        var connection = registry.Get(Context.ConnectionId) ?? throw new HubException("Register POS first.");
        try
        {
            var scan = await service.AdvanceAsync(Actor.TenantId, id, status, connection, results, Context.ConnectionAborted);
            await dispatcher.PublishAsync(scan, Context.ConnectionAborted);
            return scan;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Diagnostic result rejected. Scan {id} Status {status} Reason: {ex}");
            logger.LogWarning("Diagnostic result rejected. Scan {ScanId} Status {Status} Reason {ReasonType}: {ReasonMessage}",
                id, status, ex.GetType().Name, ex.Message);
            throw new HubException("Diagnostic result rejected. Check current assignment and scan expiry.");
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        registry.Remove(Context.ConnectionId);
        monitors.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

public sealed class TillDiagnosticDispatcher(IHubContext<TillRuntimeHub> hub, ITillConnectionRegistry registry,
    HardwareMonitorRegistry monitors, ITillDiagnosticService service, IAuthSessionValidator sessions)
{
    private async Task<TillRuntimeConnection?> TargetAsync(Guid tenant, Guid till, CancellationToken ct)
    {
        foreach (var connection in registry.ForTill(tenant, till, service.Now))
        {
            if (await service.IsCurrentAsync(connection, ct)) return connection;
            registry.Remove(connection.ConnectionId);
        }
        return null;
    }
    public async Task<TillRuntimeStatus> StatusAsync(TenantRequestContext actor, Guid till, CancellationToken ct)
    {
        await service.AuthorizeAsync(actor, till, false, ct);
        var target = await TargetAsync(actor.TenantId, till, ct);
        return new(till, target?.Binding.PosDeviceId, target == null ? "OFFLINE" : "ONLINE", target?.LastSeenAt);
    }
    public async Task<TillDiagnosticScan> StartAsync(TenantRequestContext actor, Guid till, Guid requestId, CancellationToken ct)
    {
        await service.AuthorizeAsync(actor, till, true, ct);
        var target = await TargetAsync(actor.TenantId, till, ct);
        var scan = await service.CreateAsync(actor, till, requestId, target, ct);
        if (scan.Status == "REQUESTED" && target != null)
        {
            scan = await service.AdvanceAsync(actor.TenantId, scan.Id, "DISPATCHED", target, null, ct);
            try { await hub.Clients.Client(target.ConnectionId).SendAsync("RunDeviceScan", scan, ct); }
            catch { scan = await service.AdvanceAsync(actor.TenantId, scan.Id, "FAILED", null, null, CancellationToken.None); }
        }
        await PublishAsync(scan, ct);
        return scan;
    }
    public async Task PublishAsync(TillDiagnosticScan scan, CancellationToken ct)
    {
        foreach (var monitor in monitors.ForTill(scan.TenantId, scan.TillId))
        {
            if (!await sessions.IsCurrentSessionActiveAsync(monitor.Principal, ct)) { monitors.Remove(monitor.ConnectionId); continue; }
            try { await service.AuthorizeAsync(monitor.Actor, scan.TillId, false, ct); }
            catch (UnauthorizedAccessException) { monitors.Remove(monitor.ConnectionId); continue; }
            await hub.Clients.Client(monitor.ConnectionId).SendAsync("TillHardwareScanUpdated", scan, ct);
        }
    }
    public async Task PublishRuntimeAsync(CancellationToken ct)
    {
        foreach (var monitor in monitors.All)
        {
            if (!await sessions.IsCurrentSessionActiveAsync(monitor.Principal, ct)) { monitors.Remove(monitor.ConnectionId); continue; }
            try
            {
                var status = await StatusAsync(monitor.Actor, monitor.TillId, ct);
                await hub.Clients.Client(monitor.ConnectionId).SendAsync("TillRuntimeUpdated", status, ct);
            }
            catch (UnauthorizedAccessException) { monitors.Remove(monitor.ConnectionId); }
        }
    }
}

public sealed class TillRuntimeAuthFilter(IAuthSessionValidator sessions) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (context.Context.User == null || !await sessions.IsCurrentSessionActiveAsync(context.Context.User, context.Context.ConnectionAborted))
        { context.Context.Abort(); throw new HubException("Session revoked."); }
        return await next(context);
    }
}

public sealed class TillDiagnosticTimeoutWorker(IServiceScopeFactory scopes, ILogger<TillDiagnosticTimeoutWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITillDiagnosticService>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<TillDiagnosticDispatcher>();
                foreach (var scan in await service.ExpireAsync(stoppingToken)) await dispatcher.PublishAsync(scan, stoppingToken);
                await dispatcher.PublishRuntimeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { logger.LogWarning("Hardware diagnostic timeout processing failed; retrying next interval."); }
        }
    }
}
