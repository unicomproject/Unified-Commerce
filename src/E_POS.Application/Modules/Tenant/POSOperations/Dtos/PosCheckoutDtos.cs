namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosCheckoutLineRequestDto(
    Guid VariantId,
    int Qty);

public sealed record PosCheckoutSummaryRequestDto(
    Guid DeviceId,
    string? SaleType,
    Guid? CustomerId,
    IReadOnlyList<PosCheckoutLineRequestDto> Lines,
    Guid? DiscountApplicationId = null);

public sealed record PosCheckoutBillingSummaryDto(
    int ItemCount,
    int Subtotal,
    int Discount,
    int Tax,
    int TotalPayable,
    string Currency);

public sealed record PosCheckoutSaleDetailsDto(
    string SaleType,
    int ItemsInCart,
    DateTimeOffset SaleDate,
    string CashierName);

public sealed record PosCheckoutSummaryResponseDto(
    PosCheckoutBillingSummaryDto BillingSummary,
    PosCheckoutSaleDetailsDto SaleDetails,
    IReadOnlyList<string> PaymentMethods,
    IReadOnlyList<string> ValidationMessages);

public sealed record PosCheckoutStartPaymentRequestDto(
    Guid DeviceId,
    string? SaleType,
    Guid? CustomerId,
    IReadOnlyList<PosCheckoutLineRequestDto> Lines,
    string PaymentMethod,
    int? CashReceived,
    Guid? DiscountApplicationId = null,
    string? IdempotencyKey = null,
    Guid? CardOperationId = null,
    IReadOnlyList<PosCheckoutTenderRequestDto>? Tenders = null);

public sealed record PosCheckoutTenderRequestDto(
    string MethodCode,
    int Amount,
    int? AmountTendered = null,
    Guid? CardOperationId = null);

public sealed record PosCheckoutStartPaymentLineResponseDto(
    string Name,
    int Qty,
    int UnitPrice,
    int LineTotal,
    string? Sku,
    Guid SaleLineId = default,
    int DiscountAmount = 0);

public sealed record PosReceiptTenderLineDto(
    Guid PaymentId,
    string MethodCode,
    string MethodName,
    string MethodType,
    int Amount,
    int? AmountTendered,
    int? ChangeAmount,
    string Currency,
    string Status,
    DateTimeOffset PaidAt,
    string? ProviderName = null,
    string? CardBrand = null,
    string? MaskedCardLast4 = null,
    string? AuthorizationReference = null,
    string? TerminalReference = null);

public sealed record PosReceiptDiscountLineDto(
    string Scope,
    Guid? SaleLineId,
    string Name,
    string? Code,
    string? PromotionReference,
    int Amount);

public sealed record PosReceiptTaxLineDto(
    string TaxCode,
    string TaxName,
    decimal? Rate,
    int TaxableAmount,
    int TaxAmount);

public sealed record PosReceiptCopyPolicyDto(
    int CustomerCopyCount,
    int MerchantCopyCount,
    bool PrintCustomerCopy,
    bool PrintMerchantCopy,
    bool TerminalSlipExpected,
    bool TerminalSlipPrintedByExternalTerminal);

public sealed record PosCheckoutStartPaymentResponseDto(
    Guid CheckoutSessionId,
    Guid SaleId,
    string SaleNumber,
    string ReceiptNumber,
    string BarcodeValue,
    int Subtotal,
    int DiscountTotal,
    int TaxTotal,
    int GrandTotal,
    int CashReceived,
    int ChangeDue,
    string PaymentMethod,
    string Currency,
    string SaleStatus,
    string NextAction,
    DateTimeOffset CompletedAt,
    Guid PaymentId,
    IReadOnlyList<PosCheckoutStartPaymentLineResponseDto> Items,
    Guid ReceiptId = default,
    string? MerchantName = null,
    string? OutletName = null,
    Guid TillId = default,
    string? TillName = null,
    Guid CashierId = default,
    string? CashierName = null,
    IReadOnlyList<PosReceiptTenderLineDto>? Tenders = null,
    IReadOnlyList<PosReceiptDiscountLineDto>? DiscountLines = null,
    IReadOnlyList<PosReceiptTaxLineDto>? TaxLines = null,
    PosReceiptCopyPolicyDto? CopyPolicy = null,
    string? TaxRegistrationNumber = null,
    string? TaxInvoiceLabel = null);
