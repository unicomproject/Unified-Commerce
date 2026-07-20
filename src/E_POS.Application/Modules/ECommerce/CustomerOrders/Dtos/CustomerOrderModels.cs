namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed class CustomerOrderListReadModel
{
    public IReadOnlyList<CustomerOrderSummaryReadModel> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class CustomerOrderSummaryReadModel
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string DisplayOrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public DateTimeOffset? PlacedAt { get; init; }
    public DateTimeOffset? RequestedCollectionAt { get; init; }
    public DateTimeOffset? RequestedCollectionEndAt { get; init; }
    public string? CollectionTimezone { get; init; }
    public Guid? OutletId { get; init; }
    public string? OutletName { get; init; }
    public int ItemCount { get; init; }
    public decimal GrandTotal { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string PaymentLabel { get; init; } = string.Empty;
    public IReadOnlyList<string> ThumbnailUrls { get; init; } = [];
}

public sealed class CustomerOrderDetailReadModel
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string DisplayOrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusMessage { get; init; } = string.Empty;
    public bool CanShowCollectionQr { get; init; }
    public string? CollectionQr { get; init; }
    public DateTimeOffset? PlacedAt { get; init; }
    public DateTimeOffset? RequestedCollectionAt { get; init; }
    public DateTimeOffset? RequestedCollectionEndAt { get; init; }
    public string? CollectionTimezone { get; init; }
    public Guid? OutletId { get; init; }
    public string? OutletName { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public string PaymentLabel { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal CollectionChargeAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public bool IsTaxInclusive { get; init; }
    public IReadOnlyList<CustomerOrderTimelineStepReadModel> TimelineSteps { get; init; } = [];
    public IReadOnlyList<string> AvailableActions { get; init; } = [];
    public IReadOnlyList<CustomerOrderDetailItemReadModel> Items { get; init; } = [];
}

public sealed class CustomerOrderTimelineStepReadModel
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public DateTimeOffset? OccurredAt { get; init; }
    public string Icon { get; init; } = string.Empty;
    public string? BadgeLabel { get; init; }
}

public sealed class CustomerOrderDetailItemReadModel
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? Sku { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class ClickCollectOrderStatusUpdateRequest
{
    public string Status { get; init; } = string.Empty;
}

public sealed class CustomerOrderCancelRequest
{
    public string? Reason { get; init; }
}

public sealed class CustomerOrderCancelResponse
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public DateTimeOffset CancelledAt { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ClickCollectOrderStatusUpdateResponse
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string FulfillmentStatus { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; }
    public bool CollectionQrAvailable { get; init; }
}
