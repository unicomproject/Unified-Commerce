namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed class PosOnlineOrderCollectionValidateRequest
{
    public string Token { get; init; } = string.Empty;
}

public sealed class PosOnlineOrderCollectionCompleteRequest
{
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderCollectionItemDto
{
    public string ProductName { get; init; } = string.Empty;
    public decimal QuantityPacked { get; init; }
}

public sealed class PosOnlineOrderCollectionValidateResponse
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public Guid OutletId { get; init; }
    public string? OutletName { get; init; }
    public DateTimeOffset? CollectionWindowStart { get; init; }
    public DateTimeOffset? CollectionWindowEnd { get; init; }
    public string FulfillmentStatus { get; init; } = string.Empty;
    public string PickupStatus { get; init; } = string.Empty;
    public DateTimeOffset? ReadyAt { get; init; }
    public DateTimeOffset? CollectedAt { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public bool CanCollect { get; init; }
    public bool CanTakePayment { get; init; }
    public long ExpectedVersion { get; init; }
    public string PickupNumber { get; init; } = string.Empty;
    public IReadOnlyList<PosOnlineOrderCollectionItemDto> Items { get; init; } = [];
}

public sealed class PosOnlineOrderCollectionCompleteResponse
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string PickupStatus { get; init; } = string.Empty;
    public string FulfillmentStatus { get; init; } = string.Empty;
    public string SalesOrderStatus { get; init; } = string.Empty;
    public string SalesFulfillmentStatus { get; init; } = string.Empty;
    public DateTimeOffset? CollectedAt { get; init; }
    public DateTimeOffset? FulfilledAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public long FulfillmentVersion { get; init; }
    public bool AlreadyCollected { get; init; }
}
