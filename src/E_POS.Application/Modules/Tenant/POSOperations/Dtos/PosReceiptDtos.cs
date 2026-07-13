namespace E_POS.Application.Modules.Tenant.POSOperations.Dtos;

public sealed record PosReceiptPrintRequestDto(
    string? Status,
    int Copies,
    Guid? PrinterDeviceId);

public sealed record PosReceiptPrintResponseDto(
    Guid SaleId,
    Guid ReceiptId,
    string ReceiptNumber,
    int AttemptNumber,
    string PrintStatus,
    int Copies,
    DateTimeOffset? PrintedAt);
