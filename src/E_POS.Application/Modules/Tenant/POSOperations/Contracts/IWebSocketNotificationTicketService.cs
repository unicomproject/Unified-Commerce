using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public sealed record WebSocketNotificationTicketResponse(
    string Ticket,
    int ExpiresInSeconds);

public sealed record WebSocketTicketContext(
    Guid TenantId,
    Guid UserId,
    DateTimeOffset ExpiresAtUtc);

public interface IWebSocketNotificationTicketService
{
    Task<WebSocketNotificationTicketResponse> CreateTicketAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);

    bool TryConsumeTicket(string ticket, out WebSocketTicketContext? ticketContext);
}
