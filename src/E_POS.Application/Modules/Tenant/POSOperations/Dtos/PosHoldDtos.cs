namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosCreateHoldRequestDto(
    Guid DeviceId,
    string? SaleType,
    Guid? CustomerId,
    IReadOnlyList<PosCheckoutLineRequestDto> Lines,
    string? Reason,
    Guid? DiscountApplicationId = null,
    string? IdempotencyKey = null,
    DateTimeOffset? ExpiresAt = null);

public sealed record PosRecallHoldRequestDto(Guid DeviceId);

public sealed record PosRecallHoldResponseDto(
    Guid HoldId,
    Guid SaleId,
    string HoldNumber,
    Guid DeviceId,
    Guid? CustomerId,
    string? CustomerName,
    string SaleType,
    string? Reason,
    DateTimeOffset RecalledAt,
    IReadOnlyList<PosCheckoutLineRequestDto> Lines,
    PosCheckoutSummaryResponseDto CheckoutSummary);

public sealed record PosHoldLineDto(
    Guid LineId,
    Guid? VariantId,
    string Name,
    string? VariantName,
    string? Sku,
    int Qty,
    int UnitPrice,
    int LineTotal);

public sealed record PosHoldListItemDto(
    Guid HoldId,
    string HoldNumber,
    Guid SaleId,
    string SaleNumber,
    Guid? TillId,
    Guid? TillSessionId,
    Guid? CustomerId,
    string? CustomerName,
    string? Reason,
    string Status,
    int ItemCount,
    int Subtotal,
    int Discount,
    int Tax,
    int Total,
    string Currency,
    DateTimeOffset HeldAt,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<PosHoldLineDto> Lines);

public sealed record PosHoldListResponseDto(
    IReadOnlyList<PosHoldListItemDto> Holds,
    int TotalCount);
