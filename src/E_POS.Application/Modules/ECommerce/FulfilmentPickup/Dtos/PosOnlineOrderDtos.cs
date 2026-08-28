namespace E_POS.Application.Modules.ECommerce.FulfilmentPickup.Dtos;

public sealed record PosOnlineOrderListQuery(
    Guid OutletId,
    string? Search,
    string? Status,
    string? SortBy,
    string? SortDirection,
    int Page,
    int PageSize);

public sealed record PosOnlineOrderSummaryDto(
    int NewCount,
    int PreparingCount,
    int ReadyCount,
    int DelayedCount,
    int CollectedCount,
    int CancelledCount);

public sealed record PosOnlineOrderProductPreviewDto(
    Guid ProductId,
    Guid? ProductVariantId,
    string ProductName,
    string? ImageUrl,
    string? AltText);

public sealed record PosOnlineOrderListItemDto(
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
    IReadOnlyList<PosOnlineOrderProductPreviewDto> ProductPreviews,
    int RemainingPreviewCount,
    DateTimeOffset? PlacedAt,
    DateTimeOffset UpdatedAt);

public sealed record PosOnlineOrderListDto(
    IReadOnlyList<PosOnlineOrderListItemDto> Items,
    PosOnlineOrderSummaryDto Summary,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    DateTimeOffset ServerTime);

public sealed record PosOnlineOrderLineDto(
    Guid Id,
    int LineNumber,
    string ProductName,
    string? VariantName,
    string? Sku,
    string? Barcode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string LineStatus,
    decimal PickedQuantity,
    decimal PackedQuantity);

public sealed record PosOnlineOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string? ExternalReference,
    Guid OutletId,
    string? OutletName,
    Guid? CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    DateTimeOffset? CollectionStart,
    DateTimeOffset? CollectionEnd,
    string? CollectionTimezone,
    string Status,
    string StatusLabel,
    string PaymentStatus,
    string CurrencyCode,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ChargeAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    string? CustomerNote,
    string? InternalNote,
    Guid? FulfillmentOrderId,
    Guid? AssignedToTenantUserId,
    IReadOnlyList<PosOnlineOrderLineDto> Lines,
    DateTimeOffset? PlacedAt,
    DateTimeOffset UpdatedAt);

public sealed record PosStartFulfillmentDto(
    Guid OrderId,
    Guid FulfillmentOrderId,
    string FulfillmentNumber,
    string Status,
    Guid AssignedToTenantUserId,
    DateTimeOffset StartedAt,
    bool AlreadyStarted);

public sealed record PosPickingLineDto(
    Guid Id,
    int LineNumber,
    string ProductName,
    string? VariantName,
    string? Sku,
    string? Barcode,
    decimal RequestedQuantity,
    decimal PickedQuantity,
    string Status,
    string? LocationCode,
    string? LocationName);

public sealed record PosPickingOrderDto(
    Guid OrderId,
    string OrderNumber,
    Guid FulfillmentOrderId,
    string FulfillmentNumber,
    string Status,
    Guid AssignedToTenantUserId,
    string AssignedToName,
    string CustomerName,
    DateTimeOffset? CollectionAt,
    int TotalLines,
    int PickedLines,
    IReadOnlyList<PosPickingLineDto> Lines);

public sealed record PosPickLineRequest(decimal Quantity, string? Barcode, string InputMethod);
public sealed record PosReportPickingIssueRequest(string Reason, string? Note);
public sealed record PosPackOrderRequest(string? PackingNote);
public sealed record PosFulfillmentCommandDto(
    Guid OrderId, Guid FulfillmentOrderId, string Status, int TotalLines,
    int CompletedLines, string? PackageNumber, DateTimeOffset UpdatedAt);
