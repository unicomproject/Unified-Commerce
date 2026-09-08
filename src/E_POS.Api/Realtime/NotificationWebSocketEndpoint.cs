using System.Net.WebSockets;
using E_POS.Api.Common;
using E_POS.Infrastructure.Modules.Shared.Realtime;

namespace E_POS.Api.Realtime;

public static class NotificationWebSocketEndpoint
{
    private const string RoutePattern = "/ws/notifications";
    private const int ReceiveBufferSize = 4 * 1024;

    public static IEndpointRouteBuilder MapNotificationWebSocket(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map(RoutePattern, HandleAsync).RequireAuthorization("TenantOnly");
        return endpoints;
    }

    public static bool IsWebSocketNotificationsPath(PathString path) => path.Equals(RoutePattern, StringComparison.OrdinalIgnoreCase);

    private static async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var contextFactory = context.RequestServices.GetRequiredService<ITenantRequestContextFactory>();
        if (context.User is null || !contextFactory.TryCreate(context.User, out var tenantRequestContext))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var registry = context.RequestServices.GetRequiredService<ITenantNotificationSocketRegistry>();
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        registry.Register(tenantRequestContext.UserId, socket);

        try
        {
            var buffer = new byte[ReceiveBufferSize];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, context.RequestAborted);
                    break;
                }

                // This endpoint is push-only from the server's side; any inbound payloads are discarded.
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or the app is shutting down - normal during teardown.
        }
        catch (WebSocketException)
        {
            // Underlying transport dropped unexpectedly - normal during teardown.
        }
        finally
        {
            registry.Unregister(tenantRequestContext.UserId, socket);
        }
    }
}
