namespace E_POS.LocalPrintAgent.Models;

public sealed record ReceiptPrintRequest(
    Guid RequestId,
    string ReceiptNumber,
    DateTimeOffset PrintedAt,
    string MerchantName,
    string? OutletName,
    string? TillName,
    string? CashierName,
    string Currency,
    IReadOnlyList<ReceiptLineRequest>? Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal Total,
    string PaymentMethod,
    decimal? AmountTendered,
    decimal? Change,
    IReadOnlyList<string>? FooterLines,
    string? ApiVersion = null,
    string? ReceiptContractVersion = null,
    string? BarcodeValue = null,
    IReadOnlyList<PaymentTenderLineRequest>? Tenders = null,
    IReadOnlyList<ReceiptDiscountLineRequest>? DiscountLines = null,
    IReadOnlyList<ReceiptTaxLineRequest>? TaxLines = null,
    string? TaxRegistrationNumber = null,
    string? TaxInvoiceLabel = null,
    string? CopyType = null,
    int CopyIndex = 1,
    bool IsReprint = false,
    ReceiptCopyPolicyRequest? CopyPolicy = null,
    string? ReceiptPurpose = null,
    string? ReceiptId = null,
    string? OriginalReceiptReference = null,
    IReadOnlyList<ReceiptReferenceLineRequest>? ReferenceLines = null,
    IReadOnlyList<ReceiptSettlementLineRequest>? SettlementLines = null,
    string? PrinterConfigurationId = null,
    int? PrinterConfigurationVersion = null);

public sealed record ReceiptLineRequest(
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? SaleLineId = null,
    string? ItemGroup = null,
    decimal? DiscountAmount = null,
    decimal? TaxAmount = null,
    string? Reason = null);

public sealed record ReceiptReferenceLineRequest(string Label, string Value);

public sealed record ReceiptSettlementLineRequest(
    string Label,
    decimal Amount,
    string Currency,
    string? Method = null,
    string? SafeReference = null);

public sealed record PaymentTenderLineRequest(
    string MethodCode,
    string MethodName,
    string MethodType,
    decimal Amount,
    decimal? AmountTendered,
    decimal? ChangeAmount,
    string Currency,
    string Status,
    string? ProviderName = null,
    string? CardBrand = null,
    string? MaskedCardLast4 = null,
    string? AuthorizationReference = null,
    string? TerminalReference = null);

public sealed record ReceiptDiscountLineRequest(
    string Scope,
    string? SaleLineId,
    string Name,
    string? Code,
    string? PromotionReference,
    decimal Amount);

public sealed record ReceiptTaxLineRequest(
    string TaxCode,
    string TaxName,
    decimal? Rate,
    decimal TaxableAmount,
    decimal TaxAmount);

public sealed record ReceiptCopyPolicyRequest(
    int CustomerCopyCount,
    int MerchantCopyCount,
    bool PrintCustomerCopy,
    bool PrintMerchantCopy,
    bool TerminalSlipExpected,
    bool TerminalSlipPrintedByExternalTerminal);

public sealed record PrinterHealth(
    string AgentStatus,
    string PrinterName,
    bool PrinterExists,
    bool Ready,
    string? Detail = null,
    string? AgentVersion = null,
    string? ApiVersion = null,
    string? ReceiptContractVersion = null,
    string? PaperWidth = null,
    bool? AutoCut = null,
    int? FeedLinesBeforeCut = null,
    string? StartupTimestamp = null,
    string? SpoolerStatus = null,
    string? FailureCategory = null);

public sealed record AgentReadiness(
    bool Ready,
    string Status,
    string AgentVersion,
    string ApiVersion,
    string ReceiptContractVersion,
    bool IdempotencyStoreAccessible,
    bool PrinterExists,
    bool PrinterReady,
    string? Detail = null);

public sealed record AgentDiagnostics(
    string AgentVersion,
    string ApiVersion,
    string ReceiptContractVersion,
    long TotalPrintRequests,
    long SuccessfulSpoolSubmissions,
    long ConfirmedFailures,
    long DuplicateRequests,
    long UnknownOutcomes,
    long AuthenticationFailures,
    long OperationStatusQueries,
    long IdempotencyStoreErrors,
    long ServiceStarts,
    long DroppedLogEntries,
    double AveragePrintAgentResponseMilliseconds,
    long CurrentUnresolvedOperations,
    long CompletedRequestCount);

public sealed record PrintOperationResult(
    bool Success,
    string Code,
    string Message,
    string PrinterName,
    int BytesWritten = 0);

public sealed record PrintApiResponse(
    bool Success,
    string Code,
    string Message,
    Guid RequestId,
    bool Duplicate,
    string PrinterName,
    int BytesWritten = 0);

public sealed record ApiErrorResponse(
    bool Success,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);
