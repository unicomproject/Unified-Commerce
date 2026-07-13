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
    string? IdempotencyKey = null);

public sealed record PosCheckoutStartPaymentLineResponseDto(
    string Name,
    int Qty,
    int UnitPrice,
    int LineTotal,
    string? Sku);

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
    IReadOnlyList<PosCheckoutStartPaymentLineResponseDto> Items);
