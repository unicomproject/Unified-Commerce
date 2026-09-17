using E_POS.Application.Common.Models;
using E_POS.Infrastructure.Modules.Shared.Realtime;
using Xunit;

namespace E_POS.UnitTests.Realtime;

public sealed class WebSocketNotificationTicketServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly TenantRequestContext Context = new(TenantId, UserId, Array.Empty<string>());

    [Fact]
    public async Task CreateTicketAsync_ReturnsValidTicketAndConsumesOnce()
    {
        var service = new WebSocketNotificationTicketService();

        var response = await service.CreateTicketAsync(Context, CancellationToken.None);

        Assert.NotNull(response.Ticket);
        Assert.NotEmpty(response.Ticket);
        Assert.Equal(60, response.ExpiresInSeconds);

        // First consume succeeds
        var consumed = service.TryConsumeTicket(response.Ticket, out var ticketContext);
        Assert.True(consumed);
        Assert.NotNull(ticketContext);
        Assert.Equal(TenantId, ticketContext.TenantId);
        Assert.Equal(UserId, ticketContext.UserId);

        // Second consume fails (replay prevention)
        var replay = service.TryConsumeTicket(response.Ticket, out var replayContext);
        Assert.False(replay);
        Assert.Null(replayContext);
    }

    [Fact]
    public void TryConsumeTicket_InvalidOrEmpty_ReturnsFalse()
    {
        var service = new WebSocketNotificationTicketService();

        Assert.False(service.TryConsumeTicket("", out _));
        Assert.False(service.TryConsumeTicket("   ", out _));
        Assert.False(service.TryConsumeTicket("non-existent-ticket", out _));
    }
}
