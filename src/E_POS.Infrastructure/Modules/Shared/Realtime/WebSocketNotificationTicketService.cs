using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;

namespace E_POS.Infrastructure.Modules.Shared.Realtime;

public sealed class WebSocketNotificationTicketService : IWebSocketNotificationTicketService
{
    private const int TicketTtlSeconds = 60;
    private readonly ConcurrentDictionary<string, WebSocketTicketContext> _ticketsByHash = new();

    public Task<WebSocketNotificationTicketResponse> CreateTicketAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        CleanupExpired();

        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawTicket = Convert.ToHexString(randomBytes).ToLowerInvariant();
        var hash = ComputeHash(rawTicket);

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(TicketTtlSeconds);
        var ticketContext = new WebSocketTicketContext(
            context.TenantId,
            context.UserId,
            expiresAt);

        _ticketsByHash[hash] = ticketContext;

        return Task.FromResult(new WebSocketNotificationTicketResponse(rawTicket, TicketTtlSeconds));
    }

    public bool TryConsumeTicket(string ticket, out WebSocketTicketContext? ticketContext)
    {
        ticketContext = null;
        if (string.IsNullOrWhiteSpace(ticket))
            return false;

        var hash = ComputeHash(ticket.Trim());
        if (!_ticketsByHash.TryRemove(hash, out var stored))
            return false;

        if (stored.ExpiresAtUtc < DateTimeOffset.UtcNow)
            return false;

        ticketContext = stored;
        return true;
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, value) in _ticketsByHash)
        {
            if (value.ExpiresAtUtc < now)
            {
                _ticketsByHash.TryRemove(key, out _);
            }
        }
    }

    private static string ComputeHash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
