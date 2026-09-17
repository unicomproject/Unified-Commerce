using System.Net.WebSockets;
using E_POS.Api.Common;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Infrastructure.Modules.Shared.Realtime;

namespace E_POS.Api.Realtime;

public static class NotificationWebSocketEndpoint
{
    private const string RoutePattern = "/ws/notifications";
    private const int ReceiveBufferSize = 4 * 1024;

    public static IEndpointRouteBuilder MapNotificationWebSocket(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map(RoutePattern, HandleAsync);
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

        Guid targetUserId;
        var ticket = context.Request.Query["ticket"].ToString();

        if (!string.IsNullOrEmpty(ticket))
        {
            var ticketService = context.RequestServices.GetRequiredService<IWebSocketNotificationTicketService>();
            if (!ticketService.TryConsumeTicket(ticket, out var ticketContext) || ticketContext is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            targetUserId = ticketContext.UserId;
        }
        else
        {
            var contextFactory = context.RequestServices.GetRequiredService<ITenantRequestContextFactory>();
            if (context.User is null || !contextFactory.TryCreate(context.User, out var tenantRequestContext))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            targetUserId = tenantRequestContext.UserId;
        }

        var registry = context.RequestServices.GetRequiredService<ITenantNotificationSocketRegistry>();
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        registry.Register(targetUserId, socket);

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

                // Push-only from server: any inbound frames are discarded.
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            registry.Unregister(targetUserId, socket);
        }
    }
}
