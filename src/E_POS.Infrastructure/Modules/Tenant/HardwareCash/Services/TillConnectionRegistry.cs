using System.Collections.Concurrent;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

/// <summary>Single-node registry. Replace behind ITillConnectionRegistry for distributed deployment.</summary>
public sealed class TillConnectionRegistry : ITillConnectionRegistry
{
    private readonly ConcurrentDictionary<string, TillRuntimeConnection> _connections = new();
    private readonly object _gate = new();
    public void Register(TillRuntimeConnection connection)
    {
        lock (_gate)
        {
            // Last authenticated registration wins; old sockets cannot submit results.
            foreach (var old in _connections.Values.Where(x => x.Binding.TenantId == connection.Binding.TenantId &&
                x.Binding.PosDeviceId == connection.Binding.PosDeviceId)) _connections.TryRemove(old.ConnectionId, out _);
            _connections[connection.ConnectionId] = connection;
        }
    }
    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);
    public TillRuntimeConnection? Get(string connectionId) => _connections.GetValueOrDefault(connectionId);
    public void Touch(string connectionId, DateTimeOffset now)
    {
        if (_connections.TryGetValue(connectionId, out var current))
            _connections.TryUpdate(connectionId, current with { LastSeenAt = now }, current);
    }
    public IReadOnlyList<TillRuntimeConnection> ForTill(Guid tenantId, Guid tillId, DateTimeOffset now) =>
        _connections.Values.Where(x => x.Binding.TenantId == tenantId && x.Binding.TillId == tillId &&
            x.LastSeenAt >= now.AddSeconds(-60)).OrderByDescending(x => x.ConnectedAt).ToArray();
}
