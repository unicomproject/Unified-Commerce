using System.Security.Claims;
using E_POS.Api.Realtime;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace E_POS.ApiTests.HardwareCash;

public sealed class TillDiagnosticDispatchTests
{
    [Fact]
    public async Task FiveConnectedTills_OnlyRequestedTillReceivesScan_NoBroadcast()
    {
        var service = new Service(); var registry = new TillConnectionRegistry(); var hub = new HubContext();
        var actor = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), ["tenant.hardware.manage"]);
        var connections = Enumerable.Range(1, 5).Select(i => new TillRuntimeConnection($"Till-{i:D2}", actor,
            new(actor.TenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "proof-hash"), service.Now, service.Now)).ToArray();
        foreach (var connection in connections) registry.Register(connection);
        var dispatcher = new TillDiagnosticDispatcher(hub, registry, new(), service, new Sessions());
        var scan = await dispatcher.StartAsync(actor, connections[2].Binding.TillId, Guid.NewGuid(), default);
        Assert.Equal("DISPATCHED", scan.Status);
        var sent = Assert.Single(hub.Calls);
        Assert.Equal("Till-03", sent.Connection);
        Assert.Equal("RunDeviceScan", sent.Method);
        Assert.Equal(connections[2].Binding.PosDeviceId, Assert.IsType<TillDiagnosticScan>(sent.Args[0]).PosDeviceId);
    }

    [Fact]
    public async Task StaleOrReassignedBindingIsNotSentACommand()
    {
        var service = new Service { Current = false }; var registry = new TillConnectionRegistry(); var hub = new HubContext();
        var actor = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), ["tenant.hardware.manage"]);
        var till = Guid.NewGuid();
        registry.Register(new("stale", actor, new(actor.TenantId, Guid.NewGuid(), till, Guid.NewGuid(), Guid.NewGuid(), "hash"), service.Now, service.Now));
        var dispatcher = new TillDiagnosticDispatcher(hub, registry, new(), service, new Sessions());
        var scan = await dispatcher.StartAsync(actor, till, Guid.NewGuid(), default);
        Assert.Equal("TILL_OFFLINE", scan.Status); Assert.Empty(hub.Calls); Assert.Null(registry.Get("stale"));
    }

    [Fact]
    public async Task ResultsGoOnlyToAuthorizedTenantAndTillMonitors()
    {
        var service = new Service(); var hub = new HubContext(); var monitors = new HardwareMonitorRegistry();
        var actor = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), ["tenant.hardware.manage"]);
        var till = Guid.NewGuid();
        monitors.Add(new("allowed", actor, till, new()));
        monitors.Add(new("other-till", actor, Guid.NewGuid(), new()));
        monitors.Add(new("other-tenant", actor with { TenantId = Guid.NewGuid() }, till, new()));
        monitors.Add(new("revoked", actor with { Permissions = [] }, till, new()));
        var scan = await service.CreateAsync(actor, till, Guid.NewGuid(), null, default);
        var dispatcher = new TillDiagnosticDispatcher(hub, new TillConnectionRegistry(), monitors, service, new Sessions());
        await dispatcher.PublishAsync(scan, default);
        Assert.Equal("allowed", Assert.Single(hub.Calls).Connection);
    }
    private sealed class Sessions : IAuthSessionValidator
    { public Task<bool> IsCurrentSessionActiveAsync(ClaimsPrincipal p, CancellationToken ct) => Task.FromResult(true); }
    private sealed class Service : ITillDiagnosticService
    {
        public DateTimeOffset Now { get; } = DateTimeOffset.UtcNow;
        public bool Current { get; init; } = true;
        private TillDiagnosticScan? _scan;
        public Task<Guid> AuthorizeAsync(TenantRequestContext a, Guid t, bool manage, CancellationToken ct) =>
            a.HasPermission("tenant.hardware.manage") ? Task.FromResult(Guid.NewGuid()) : throw new UnauthorizedAccessException();
        public Task<bool> IsCurrentAsync(TillRuntimeConnection c, CancellationToken ct) => Task.FromResult(Current);
        public Task<TillRuntimeBinding> BindAsync(TenantRequestContext a, string p, CancellationToken ct) => throw new NotSupportedException();
        public Task<TillDiagnosticScan> CreateAsync(TenantRequestContext a, Guid till, Guid request, TillRuntimeConnection? c, CancellationToken ct)
        {
            _scan = new(Guid.NewGuid(), a.TenantId, c?.Binding.OutletId ?? Guid.NewGuid(), till, c?.Binding.PosDeviceId ?? Guid.Empty,
                c?.Binding.AssignmentId ?? Guid.Empty, a.UserId, Now, Now.AddSeconds(60), c == null ? "TILL_OFFLINE" : "REQUESTED", [], []);
            return Task.FromResult(_scan);
        }
        public Task<TillDiagnosticScan> AdvanceAsync(Guid tenant, Guid id, string status, TillRuntimeConnection? c, IReadOnlyList<TillDiagnosticResult>? results, CancellationToken ct) =>
            Task.FromResult(_scan! with { Status = status });
        public Task<TillDiagnosticScan?> GetAsync(TenantRequestContext a, Guid t, Guid id, CancellationToken ct) => Task.FromResult(_scan);
        public Task<IReadOnlyList<TillDiagnosticScan>> ExpireAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<TillDiagnosticScan>>([]);
    }
    private sealed class HubContext : IHubContext<TillRuntimeHub>
    {
        public List<(string Connection, string Method, object?[] Args)> Calls { get; } = [];
        public IHubClients Clients => new ClientsImpl(Calls);
        public IGroupManager Groups => throw new NotSupportedException("No client-controlled groups");
    }
    private sealed class ClientsImpl(List<(string Connection, string Method, object?[] Args)> calls) : IHubClients
    {
        public IClientProxy Client(string connectionId) => new Proxy(connectionId, calls);
        public IClientProxy All => throw new InvalidOperationException("Broadcast forbidden");
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new InvalidOperationException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new InvalidOperationException();
        public IClientProxy Group(string groupName) => throw new InvalidOperationException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new InvalidOperationException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new InvalidOperationException();
        public IClientProxy User(string userId) => throw new InvalidOperationException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new InvalidOperationException();
    }
    private sealed class Proxy(string id, List<(string Connection, string Method, object?[] Args)> calls) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        { calls.Add((id, method, args)); return Task.CompletedTask; }
    }
}
