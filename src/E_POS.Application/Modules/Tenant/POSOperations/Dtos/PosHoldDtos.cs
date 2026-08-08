namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

/// <param name="ExpiresAt">
/// Obsolete/ignored: the server always derives the Park expiry window (24h from
/// <c>heldAt</c>). A client-supplied value can never override it. Retained only for
/// wire-format backward compatibility; see <see cref="Services.PosHoldService"/>.
/// </param>
/// <param name="SourceSaleId">
/// Optional originating sale id when parking from an in-progress checkout. When
/// present, the source sale must not already have a partial/complete payment
/// recorded, otherwise the hold is rejected with
/// <c>pos_holds.sale_partially_paid_cannot_be_parked</c>.
/// </param>
public sealed record PosCreateHoldRequestDto(
    Guid DeviceId,
    string? SaleType,
    Guid? CustomerId,
    IReadOnlyList<PosCheckoutLineRequestDto> Lines,
    string? Reason,
    Guid? DiscountApplicationId = null,
    string? IdempotencyKey = null,
    DateTimeOffset? ExpiresAt = null,
    Guid? SourceSaleId = null);

public sealed record PosRecallHoldRequestDto(Guid DeviceId);

public static class PosHoldListScopes
{
    public const string Today = "today";
    public const string CurrentShift = "current-shift";
    public const string AllActive = "all-active";
}

public sealed record PosHoldListQueryDto(
    Guid DeviceId,
    string Scope = PosHoldListScopes.Today,
    int Page = 1,
    int PageSize = 25);

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
    PosCheckoutSummaryResponseDto CheckoutSummary,
    IReadOnlyList<string> StockWarnings);

public sealed record PosHoldLineDto(
    Guid LineId,
    Guid? VariantId,
    string Name,
    string? VariantName,
    string? Sku,
    int Qty,
    int UnitPrice,
    int LineTotal,
    string? LineNote = null,
    string? ImageUrl = null);

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
    int TotalCount,
    int TotalValue,
    string Currency,
    int Page,
    int PageSize);
