namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed class PosOnlineOrderPickupVerifyRequest
{
    public string PickupCode { get; init; } = string.Empty;
}

public sealed class PosOnlineOrderPickupVerifyResponse
{
    public Guid OrderId { get; init; }
    public Guid PickupOrderId { get; init; }
    public string PickupStatus { get; init; } = string.Empty;
    public DateTimeOffset VerifiedAt { get; init; }
    public int RemainingAttempts { get; init; }
}

public sealed class PosOnlineOrderPickupCollectResponse
{
    public Guid OrderId { get; init; }
    public Guid PickupOrderId { get; init; }
    public string PickupStatus { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public DateTimeOffset CollectedAt { get; init; }
}
