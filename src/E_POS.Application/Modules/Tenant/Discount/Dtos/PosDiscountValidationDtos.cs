using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.Discount.Dtos;

public sealed record PosDiscountValidationRequestDto(
    Guid DeviceId,
    Guid? DiscountId,
    string DiscountSource,
    decimal? RequestedValue,
    string? Scope,
    Guid? TargetVariantId,
    string? Reason,
    string? SaleType,
    Guid? CustomerId,
    IReadOnlyList<PosCheckoutLineRequestDto> Lines,
    string? IdempotencyKey = null,
    string? CalculationMethod = null);

public sealed record PosDiscountCatalogQueryDto(
    Guid DeviceId,
    string? Scope,
    Guid? VariantId,
    IReadOnlyList<Guid>? VariantIds,
    Guid? CustomerId,
    decimal? Quantity,
    decimal? CartSubtotal,
    string? CurrencyCode);

public sealed record PosDiscountCancelRequestDto(Guid DeviceId, string? Reason);

public sealed record PosDiscountCancelResponseDto(
    Guid ApplicationId,
    string Status,
    DateTimeOffset CancelledAt);

public sealed record PosDiscountValidationResponseDto(
    Guid DiscountId,
    bool IsValid,
    string Outcome,
    string CalculationMethod,
    decimal RequestedValue,
    decimal CashierLimit,
    decimal AbsoluteLimit,
    int Subtotal,
    int EligibleSubtotal,
    int DiscountAmount,
    int TotalAfterDiscount,
    string CurrencyCode,
    bool RequiresManagerApproval,
    string CartHash,
    IReadOnlyList<string> ValidationMessages);

public sealed record PosDiscountApplyResponseDto(
    Guid ApplicationId,
    Guid DiscountId,
    bool Applied,
    string Status,
    int Subtotal,
    int DiscountAmount,
    int TotalAfterDiscount,
    bool RequiresManagerApproval,
    DateTimeOffset ExpiresAt,
    string CartHash,
    IReadOnlyList<string> Messages);

public sealed record PosDiscountDecisionRequestDto(
    string Decision,
    string? Note);

public sealed record PosDiscountDecisionResponseDto(
    Guid ApplicationId,
    Guid DiscountId,
    string Status,
    Guid DecidedBy,
    DateTimeOffset DecidedAt,
    string? DecisionNote,
    int DiscountAmount,
    int TotalAfterDiscount,
    string CartHash);
