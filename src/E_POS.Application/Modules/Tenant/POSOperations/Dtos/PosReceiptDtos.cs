using System.Text.Json;

namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosReceiptPrintRequestDto(
    string? Status,
    int Copies,
    Guid? PrinterDeviceId,
    Guid? DeviceId = null,
    Guid? TillId = null,
    Guid? CashierUserId = null,
    string? PrinterTransport = null,
    string? ConfiguredPrinterName = null,
    Guid? PrintRequestId = null,
    DateTimeOffset? RequestedAt = null,
    string? AgentResult = null,
    string? FailureCategory = null,
    bool IsRetry = false,
    bool IsReprint = false,
    string? ClientCorrelationId = null,
    Guid? ReprintOperationId = null,
    string? ReprintReasonCode = null,
    string? ReprintReasonNote = null,
    string? CopyType = null,
    int CopyIndex = 1,
    Guid? ReceiptId = null,
    string? ReceiptPurpose = null,
    Guid? PrinterConfigurationId = null,
    int? PrinterConfigurationVersion = null,
    string? RoutingPurpose = null,
    bool UnknownOutcome = false,
    Guid? RecoveryPrintRequestId = null);

public sealed record PosReceiptPrintResponseDto(
    Guid SaleId,
    Guid ReceiptId,
    string ReceiptNumber,
    int AttemptNumber,
    string PrintStatus,
    int Copies,
    DateTimeOffset? PrintedAt);

public sealed record PosReceiptSearchRequestDto(
    string? Query,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? CashierUserId,
    Guid? TillId,
    string? PaymentMethod,
    string? ReceiptType,
    string? ReceiptStatus,
    decimal? MinAmount,
    decimal? MaxAmount,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record PosReceiptHistoryItemDto(
    Guid ReceiptId,
    Guid SaleId,
    string ReceiptNumber,
    string SaleNumber,
    string ReceiptType,
    string ReceiptStatus,
    DateTimeOffset IssuedAt,
    string CashierName,
    string TillName,
    string OutletName,
    string PaymentMethod,
    string CurrencyCode,
    decimal TotalAmount,
    int ReprintCount);

public sealed record PosReceiptSearchResponseDto(
    IReadOnlyList<PosReceiptHistoryItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PosReceiptDetailLineDto(
    string Name,
    string? Sku,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    Guid SaleLineId = default);

public sealed record PosReceiptDetailDto(
    Guid ReceiptId,
    Guid SaleId,
    string ReceiptNumber,
    string SaleNumber,
    string ReceiptType,
    string ReceiptStatus,
    DateTimeOffset IssuedAt,
    string CashierName,
    Guid CashierUserId,
    string TillName,
    Guid TillId,
    string OutletName,
    Guid OutletId,
    string PaymentMethod,
    string CurrencyCode,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ChargeAmount,
    decimal RoundingAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ChangeAmount,
    IReadOnlyList<PosReceiptDetailLineDto> Items,
    int ReprintCount,
    DateTimeOffset? LastReprintedAt,
    string? MerchantName = null,
    IReadOnlyList<PosReceiptTenderLineDto>? Tenders = null,
    IReadOnlyList<PosReceiptDiscountLineDto>? DiscountLines = null,
    IReadOnlyList<PosReceiptTaxLineDto>? TaxLines = null,
    PosReceiptCopyPolicyDto? CopyPolicy = null,
    string? TaxRegistrationNumber = null,
    string? TaxInvoiceLabel = null,
    JsonElement? HistoricalSnapshot = null,
    string? ReceiptDataJson = null);

public sealed record PosReceiptReprintAuthorizationRequestDto(
    string? ReasonCode,
    string? ReasonNote);

public sealed record PosReceiptReprintAuthorizationResponseDto(
    Guid ReceiptId,
    Guid OperationId,
    bool Authorized,
    bool RequiresManagerApproval,
    string DecisionCode,
    string Message,
    DateTimeOffset AuthorizedAt);

public sealed record ResolvedReceiptTemplateDto(
    Guid TemplateVersionId,
    string TemplateData);
