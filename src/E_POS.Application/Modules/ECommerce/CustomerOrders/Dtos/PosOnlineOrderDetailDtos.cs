namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed record PosOnlineOrderListQuery(
    Guid OutletId,
    string? Search,
    string? Status,
    string? SortBy,
    string? SortDirection,
    int Page,
    int PageSize);

public sealed record PosOnlineOrderSummaryResponse(
    int NewCount,
    int PreparingCount,
    int ReadyCount,
    int DelayedCount,
    int CollectedCount,
    int CancelledCount);

public sealed record PosOnlineOrderProductPreviewResponse(
    Guid ProductId,
    Guid? ProductVariantId,
    string ProductName,
    string? ImageUrl,
    string? AltText);

public sealed record PosOnlineOrderListItemResponse(
    Guid Id,
    string OrderNumber,
    string? ExternalReference,
    string CustomerName,
    string? CustomerPhone,
    DateTimeOffset? CollectionStart,
    DateTimeOffset? CollectionEnd,
    string? CollectionTimezone,
    string Status,
    string StatusLabel,
    string PaymentStatus,
    string CurrencyCode,
    decimal TotalAmount,
    int ItemCount,
    decimal UnitCount,
    IReadOnlyList<PosOnlineOrderProductPreviewResponse> ProductPreviews,
    int RemainingPreviewCount,
    DateTimeOffset? PlacedAt,
    DateTimeOffset UpdatedAt);

public sealed record PosOnlineOrderListResponse(
    IReadOnlyList<PosOnlineOrderListItemResponse> Items,
    PosOnlineOrderSummaryResponse Summary,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    DateTimeOffset ServerTime);

public sealed class PosOnlineOrderDetailResponse
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string? ExternalReference { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public string FulfillmentStatus { get; init; } = string.Empty;
    public string? PickupStatus { get; init; }
    public DateTimeOffset? PlacedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? SalesChannel { get; init; }
    public Guid? CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerClassification { get; init; }
    public string? CustomerNote { get; init; }
    public Guid OutletId { get; init; }
    public string OutletName { get; init; } = string.Empty;
    public string? PickupNumber { get; init; }
    public DateTimeOffset? CollectionStart { get; init; }
    public DateTimeOffset? CollectionEnd { get; init; }
    public string? CollectionTimezone { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ChargeAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal UnitCount { get; init; }
    public Guid? FulfillmentOrderId { get; init; }
    public long? FulfillmentVersion { get; init; }
    public Guid? AssignedToTenantUserId { get; init; }
    public DateTimeOffset ServerTime { get; init; }
    public IReadOnlyList<PosOnlineOrderDetailLineResponse> Lines { get; init; } = [];
}

public sealed class PosOnlineOrderStartFulfillmentRequest
{
    public long ExpectedVersion { get; init; }
}

public sealed class PosOnlineOrderStartFulfillmentResponse
{
    public Guid OrderId { get; init; }
    public Guid FulfillmentOrderId { get; init; }
    public string FulfillmentNumber { get; init; } = string.Empty;
    public string FulfillmentStatus { get; init; } = string.Empty;
    public Guid AssignedToTenantUserId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public long FulfillmentVersion { get; init; }
}

public sealed class PosOnlineOrderDetailLineResponse
{
    public Guid Id { get; init; }
    public Guid SalesOrderLineId { get; init; }
    public Guid? FulfillmentOrderLineId { get; init; }
    public int LineNumber { get; init; }
    public Guid ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? LineStatus { get; init; }
    public decimal Quantity { get; init; }
    public decimal PickedQuantity { get; init; }
    public decimal PackedQuantity { get; init; }
    public decimal RemainingQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public string? ImageUrl { get; init; }
    public string? AltText { get; init; }
}
