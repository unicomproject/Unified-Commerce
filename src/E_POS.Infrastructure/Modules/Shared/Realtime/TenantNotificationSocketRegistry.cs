using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;

namespace E_POS.Infrastructure.Modules.Shared.Realtime;

// Keyed by TenantUserId (globally unique) rather than TenantId: notifications are already
// fanned out per staff recipient upstream, so a push here targets exactly one recipient's
// live connections and is never re-broadcast to the rest of the tenant's staff.
public sealed class TenantNotificationSocketRegistry : ITenantNotificationSocketRegistry, IRealtimeNotificationPublisher
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<WebSocket, byte>> _connectionsByTenantUserId = new();

    public void Register(Guid tenantUserId, WebSocket socket)
    {
        var connections = _connectionsByTenantUserId.GetOrAdd(tenantUserId, static _ => new ConcurrentDictionary<WebSocket, byte>());
        connections[socket] = 0;
    }

    public void Unregister(Guid tenantUserId, WebSocket socket)
    {
        if (!_connectionsByTenantUserId.TryGetValue(tenantUserId, out var connections))
            return;

        connections.TryRemove(socket, out _);
        if (connections.IsEmpty)
        {
            _connectionsByTenantUserId.TryRemove(tenantUserId, out _);
        }
    }

    public async Task PublishToTenantUserAsync(
        Guid tenantUserId,
        RealtimeNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        if (!_connectionsByTenantUserId.TryGetValue(tenantUserId, out var connections) || connections.IsEmpty)
            return;

        var json = JsonSerializer.Serialize(new
        {
            type = payload.EventCode,
            title = payload.Title,
            body = payload.Body,
            actionUrl = payload.ActionUrl,
            sourceReferenceId = payload.SourceReferenceId
        });
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var socket in connections.Keys)
        {
            if (socket.State != WebSocketState.Open)
                continue;

            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken);
            }
            catch
            {
                // Best-effort live push; the DB-backed inbox item from InAppNotificationChannelHandler
                // is the reliable delivery path a client catches up on when it reconnects.
            }
        }
    }
}
