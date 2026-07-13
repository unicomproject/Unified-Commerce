namespace E_POS.Application.Modules.Tenant.Discount.Dtos;

public sealed record PosDiscountAuthorityDto(
    decimal MaxPercentage,
    decimal MaxFixedAmount,
    string CurrencyCode);

public sealed record PosDiscountResponseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Scope,
    string CalculationMethod,
    decimal PredefinedValue,
    decimal AbsoluteValueLimit,
    decimal CashierValueLimit,
    string? CurrencyCode,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    decimal? MinQuantity,
    bool RequiresManagerApproval,
    bool IsStackable,
    string? StackingGroupCode,
    int Priority,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

public sealed record PosDiscountCatalogResponseDto(
    PosDiscountAuthorityDto Authority,
    IReadOnlyList<PosDiscountResponseDto> Discounts);
