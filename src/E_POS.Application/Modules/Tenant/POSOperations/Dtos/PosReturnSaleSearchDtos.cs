namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosReturnSaleSummaryDto(
    Guid SaleId,
    string InvoiceNo,
    Guid? CustomerId,
    string CustomerName,
    string Phone,
    string PaymentMethod,
    string MaskedCard,
    DateTimeOffset? SaleDate,
    decimal Total,
    int ItemCount,
    string Currency);

public sealed record PosReturnSaleSearchPageDto(
    IReadOnlyList<PosReturnSaleSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PosReturnPolicyCheckDto(
    string Label,
    string Value,
    bool Passed);

public sealed record PosReturnSaleLineEligibilityDto(
    Guid SaleLineId,
    Guid? VariantId,
    string Name,
    string Sku,
    string? ImageStorageKey,
    decimal SoldQty,
    decimal ReturnedQty,
    decimal AvailableReturnQty,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsReturnable,
    string EligibilityStatus,
    string? IneligibilityReason);

public sealed record PosReturnSaleEligibilityDto(
    Guid SaleId,
    string InvoiceNo,
    Guid? CustomerId,
    string CustomerName,
    DateTimeOffset? SaleDate,
    string PaymentMethod,
    string MaskedCard,
    string Currency,
    IReadOnlyList<PosReturnSaleLineEligibilityDto> Items,
    IReadOnlyList<PosReturnPolicyCheckDto> PolicyChecks);

public sealed record PosReturnCreditPreviewLineRequestDto(
    Guid SaleLineId,
    decimal ReturnQty);

public sealed record PosReturnCreditPreviewRequestDto(
    string ReasonCode,
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines);

public sealed record PosReturnCreditPreviewItemDto(
    Guid SaleLineId,
    string Name,
    string Sku,
    string VariantLabel,
    string? ImageStorageKey,
    decimal ReturnQty,
    decimal UnitPrice,
    decimal LineAmount);

public sealed record PosReturnCreditCalculationDto(
    decimal ItemValue,
    string DiscountLabel,
    decimal DiscountAdjustment,
    string TaxLabel,
    decimal TaxAdjustment,
    decimal NetCreditAmount);

public sealed record PosReturnCreditPreviewDto(
    Guid SaleId,
    string InvoiceNo,
    Guid? CustomerId,
    string CustomerName,
    string CustomerDisplayId,
    DateTimeOffset? SaleDate,
    string PaymentMethod,
    string MaskedCard,
    string Currency,
    decimal SaleTotal,
    int SaleItemCount,
    string ReasonCode,
    string ReasonLabel,
    IReadOnlyList<PosReturnCreditPreviewItemDto> Items,
    PosReturnCreditCalculationDto Calculation,
    string CreditReference,
    int ValidityDays,
    DateTimeOffset? ExpiresAt,
    int SelectedItemCount);

public sealed record PosReturnCompleteRequestDto(
    string ReasonCode,
    string SettlementMethodCode,
    string? Notes,
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines);

public sealed record PosReturnReceiptDto(
    Guid ReturnId,
    string ReceiptNumber,
    string OriginalInvoiceNo,
    int ReturnedItemCount,
    string SettlementMethodCode,
    string SettlementMethodLabel,
    string SettlementDisplay,
    string SettlementResult,
    string Currency,
    decimal RefundAmount,
    decimal CustomerCreditAmount,
    DateTimeOffset CompletedAt,
    string ReturnStatus,
    string CustomerName,
    string CashierName,
    string TillName,
    string ApprovalStatus,
    string CustomerAcknowledgement);
