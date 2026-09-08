using System.Net.WebSockets;

namespace E_POS.Infrastructure.Modules.Shared.Realtime;

public interface ITenantNotificationSocketRegistry
{
    void Register(Guid tenantUserId, WebSocket socket);

    void Unregister(Guid tenantUserId, WebSocket socket);
}
