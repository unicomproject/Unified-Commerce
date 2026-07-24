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

public sealed record PosReturnSaleSearchFilterDto(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? PaymentMethodCode,
    decimal? MinAmount,
    decimal? MaxAmount);

public sealed record PosReturnPaymentMethodFilterOptionDto(
    string Code,
    string Label);

public sealed record PosReturnSaleSearchPageDto(
    IReadOnlyList<PosReturnSaleSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PosReturnPaymentMethodFilterOptionDto>? PaymentMethods = null);

public sealed record PosReturnPolicyCheckDto(
    string Label,
    string Value,
    bool Passed,
    string Code = "",
    string Description = "",
    string Status = "",
    string? Severity = null,
    string? Reason = null,
    bool RequiresReview = false);

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
    string? IneligibilityReason,
    decimal? RequestedReturnQty = null,
    decimal? EligibleReturnQty = null,
    string? Barcode = null);

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
    IReadOnlyList<PosReturnPolicyCheckDto> PolicyChecks,
    string OverallStatus = "",
    bool CanContinue = false,
    int EligibleItemCount = 0,
    int SelectedItemCount = 0,
    string OverallMessage = "",
    string? PolicyNote = null,
    bool RequiresInspection = false,
    bool RequiresManagerApproval = false);

public sealed record PosReturnEligibilityCheckRequestDto(
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines);

public sealed record PosReturnReasonsValidateRequestDto(
    IReadOnlyList<PosReturnReasonAssignmentRequestDto> Items,
    bool ApplySameReasonToAll = false);

public sealed record PosReturnReasonAssignmentRequestDto(
    Guid SaleLineId,
    string ReasonCode,
    string? Notes);

public sealed record PosReturnReasonsValidateResponseDto(
    Guid SaleId,
    bool ApplySameReasonToAll,
    int NotesMaxLength,
    IReadOnlyList<PosReturnReasonAssignmentResultDto> Items);

public sealed record PosReturnReasonAssignmentResultDto(
    Guid SaleLineId,
    Guid ReasonId,
    string ReasonCode,
    string ReasonDisplayName,
    string? Notes,
    bool RequiresNotes,
    bool RequiresInspection,
    bool RequiresManagerApproval = false);

public sealed record PosReturnReasonOptionDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    int SortOrder,
    bool AppliesToReturn,
    bool AppliesToExchange,
    bool RequiresNotes,
    bool RequiresInspection,
    bool RequiresManagerApproval);

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
    int SelectedItemCount,
    bool CanProceed = true,
    bool RequiresApproval = false,
    string? PolicyMessage = null,
    int? DraftVersion = null);

public sealed record PosReturnRefundMethodOptionDto(
    string Code,
    string DisplayName,
    bool Enabled,
    string? DisabledReason,
    string? OriginalPaymentMethod,
    string? MaskedReference,
    bool RequiresOpenTill,
    bool RequiresProvider,
    bool RequiresApproval);

public sealed record PosReturnRefundMethodsResponseDto(
    IReadOnlyList<PosReturnRefundMethodOptionDto> Items,
    string? DefaultMethodCode,
    string? SelectedMethodCode,
    DateTimeOffset? SelectedAt);

public sealed record PosReturnRefundMethodSaveRequestDto(string MethodCode);

public sealed record PosReturnRefundMethodSaveResponseDto(
    Guid SaleId,
    string MethodCode,
    DateTimeOffset SelectedAt);

public sealed record PosReturnCompleteRequestDto(
    string ReasonCode,
    string SettlementMethodCode,
    string? Notes,
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines,
    int ExpectedVersion,
    string IdempotencyKey);

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
    string CustomerAcknowledgement,
    Guid? ReceiptId = null,
    Guid? OriginalSaleId = null,
    string Resolution = "REFUND",
    bool CanPrint = true,
    string? ReturnNumber = null,
    string? ExchangeNumber = null,
    Guid? SalesExchangeId = null,
    string? ReplacementOrderNumber = null,
    string? PolicyMessage = null,
    IReadOnlyList<PosReturnCompletionItemDto>? ReturnedItems = null,
    IReadOnlyList<PosReturnCompletionItemDto>? ReplacementItems = null,
    decimal? ReturnItemValue = null,
    decimal? ReplacementItemValue = null,
    decimal? DifferenceAmount = null,
    string? DifferenceDirection = null,
    Guid? OutletId = null,
    string? OutletName = null,
    Guid? TillId = null,
    Guid? DeviceId = null,
    string? DeviceName = null,
    Guid? CustomerId = null,
    string? CustomerDisplayName = null,
    Guid? ProcessedByUserId = null,
    string? ProcessedByName = null,
    string? ReceiptType = null,
    string? OriginalSaleNumber = null,
    string? CardBrand = null,
    string? MaskedCard = null,
    string? ProviderTransactionReference = null,
    string? PaymentRefundStatus = null,
    decimal? AmountPaidByCustomer = null,
    decimal? AmountRefundedToCustomer = null,
    decimal? AmountDueFromCustomer = null,
    decimal? AmountDueToCustomer = null,
    decimal? ReturnSubtotal = null,
    decimal? ReturnDiscount = null,
    decimal? ReturnTax = null,
    decimal? ReturnTotal = null,
    decimal? ReplacementSubtotal = null,
    decimal? ReplacementDiscount = null,
    decimal? ReplacementTax = null,
    decimal? ReplacementTotal = null,
    int PrintCount = 0,
    bool HasBeenPrinted = false);

public sealed record PosReturnCompletionItemDto(
    Guid? SaleLineId,
    string Name,
    string VariantLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineAmount,
    string? ImageStorageKey,
    bool IsReplacement = false,
    Guid? SalesReturnLineId = null,
    Guid? ReplacementOrderLineId = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
    string? Sku = null,
    decimal? Subtotal = null,
    decimal? Discount = null,
    decimal? Tax = null,
    decimal? Total = null,
    string? ReasonCode = null,
    string? ReasonDisplay = null,
    string? ConditionCode = null,
    string? ConditionDisplay = null,
    string? Disposition = null,
    string? Currency = null);

public sealed record PosReturnInspectionConditionDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string StatusCategory,
    int SortOrder,
    bool IsResellable,
    string RefundImpact,
    bool RequiresNotes,
    bool RequiresPhoto,
    bool RequiresApproval);

public sealed record PosReturnInspectionMediaDto(
    Guid MediaId,
    Guid SaleLineId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string MediaUrl);

public sealed record PosReturnInspectionLineRequestDto(
    Guid SaleLineId,
    string ConditionCode,
    string? Notes,
    IReadOnlyList<Guid> MediaIds);

public sealed record PosReturnInspectionValidateRequestDto(
    IReadOnlyList<PosReturnInspectionLineRequestDto> Lines,
    IReadOnlyList<PosReturnInspectionReasonRefDto>? ReasonRefs = null,
    int? Version = null);

public sealed record PosReturnInspectionReasonRefDto(
    Guid SaleLineId,
    string ReasonCode);

public sealed record PosReturnInspectionDraftSaveRequestDto(
    IReadOnlyList<PosReturnInspectionDraftLineDto> Lines,
    int? Version = null);

public sealed record PosReturnInspectionDraftLineDto(
    Guid SaleLineId,
    string ConditionCode,
    string? Notes,
    IReadOnlyList<Guid>? MediaIds = null);

public sealed record PosReturnInspectionDraftResponseDto(
    Guid DraftId,
    string Status,
    IReadOnlyList<PosReturnInspectionDraftLineDto> Lines,
    int Version = 1,
    DateTimeOffset? ExpiresAt = null);

public sealed record PosReturnResolutionSaveRequestDto(
    string ResolutionType,
    int ExpectedVersion);

public sealed record PosReturnResolutionOptionDto(
    string ResolutionType,
    bool Allowed,
    string? UnavailableReasonCode);

public sealed record PosReturnResolutionResponseDto(
    Guid SaleId,
    Guid DraftId,
    string? ResolutionType,
    DateTimeOffset? ResolutionSelectedAt,
    Guid? ResolutionSelectedByTenantUserId,
    int Version,
    string DraftStatus,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<PosReturnResolutionOptionDto> AvailableOptions,
    bool RefundAllowed,
    bool ExchangeAllowed,
    bool RequiresManagerApproval,
    bool RequiresInspection,
    bool CanChange,
    string NextStep);

public sealed record PosReturnInspectionPolicyMessageDto(
    string Severity,
    string Title,
    string Message,
    IReadOnlyList<Guid> AffectedSaleLineIds,
    bool RequiresApproval,
    string RefundImpact);

public sealed record PosReturnInspectionValidateResponseDto(
    bool CanContinue,
    int SelectedItemCount,
    int InspectedItemCount,
    int PendingItemCount,
    IReadOnlyDictionary<string, int> ConditionBreakdown,
    IReadOnlyList<PosReturnInspectionPolicyMessageDto> PolicyMessages,
    bool RequiresReview,
    int NotesMaxLength,
    int MaxPhotosPerLine,
    long MaxPhotoSizeBytes,
    Guid? DraftId = null,
    string? Status = null,
    int? Version = null,
    DateTimeOffset? ExpiresAt = null,
    bool RequiresInspection = false,
    bool RequiresManagerApproval = false);
public sealed record PosReturnInspectionMediaContentDto(
    Stream Content,
    string ContentType,
    string FileName);

public sealed record PosExchangeProductDto(
    Guid ProductId,
    Guid? VariantId,
    string Name,
    string Sku,
    string? Barcode,
    string? VariantDisplayName,
    string? ImageStorageKey,
    string StockStatus,
    decimal? AvailableQuantity,
    decimal SellingPrice,
    string CurrencyCode,
    bool HasVariants,
    bool Enabled,
    string? DisabledReason);

public sealed record PosExchangeProductsResponseDto(
    IReadOnlyList<PosExchangeProductDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    string CurrencyCode);

public sealed record PosExchangeReplacementItemRequestDto(
    Guid ReturnedSaleLineId,
    Guid ReplacementProductId,
    Guid ReplacementVariantId,
    decimal Quantity);

public sealed record PosExchangeReplacementSaveRequestDto(
    IReadOnlyList<PosExchangeReplacementItemRequestDto> Items,
    int ExpectedVersion);

public sealed record PosExchangeReplacementItemDto(
    Guid ReturnedSaleLineId,
    Guid ReplacementProductId,
    Guid ReplacementVariantId,
    string ProductName,
    string Sku,
    string? VariantDisplayName,
    string? ImageStorageKey,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string CurrencyCode,
    string StockStatus,
    decimal? AvailableQuantity,
    DateTimeOffset SelectedAt,
    decimal LineSubtotal = 0m,
    decimal LineDiscount = 0m,
    decimal LineTax = 0m,
    Guid? PriceListItemId = null);

public sealed record PosExchangeReplacementSaveResponseDto(
    Guid SaleId,
    IReadOnlyList<PosExchangeReplacementItemDto> Items,
    DateTimeOffset SelectedAt,
    int Version,
    DateTimeOffset? ExpiresAt = null);

public sealed record PosExchangePreviewRequestDto(
    string ReasonCode,
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines);

public sealed record PosExchangePreviewDto(
    Guid SaleId,
    string CurrencyCode,
    int ReturnedItemCount,
    decimal ReturnItemValue,
    decimal ReplacementItemValue,
    decimal TaxAdjustment,
    decimal DiscountAdjustment,
    decimal DifferenceAmount,
    string DifferenceDirection,
    bool CanProceed,
    bool RequiresApproval,
    IReadOnlyList<string> PolicyMessages,
    IReadOnlyList<PosExchangeReplacementItemDto> ReplacementItems,
    decimal ReplacementSubtotal = 0m,
    decimal ReplacementDiscount = 0m,
    decimal ReplacementTax = 0m,
    decimal AmountDueFromCustomer = 0m,
    decimal AmountDueToCustomer = 0m,
    int? DraftVersion = null);
