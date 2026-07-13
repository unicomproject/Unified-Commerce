using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class ReceiptPrintLog : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReceiptId { get; protected set; }
    public int AttemptNumber { get; protected set; }
    public Guid? PrinterDeviceId { get; protected set; }
    public string PrintedCopyType { get; protected set; } = string.Empty;
    public string PrintStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? PrintedAt { get; protected set; }
    public Guid? OperatorTenantUserId { get; protected set; }
    public string? ErrorCode { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public string? PrintResultJson { get; protected set; }

    public static ReceiptPrintLog Create(
        Guid id,
        Guid tenantId,
        Guid receiptId,
        int attemptNumber,
        string printedCopyType,
        string printStatus,
        DateTimeOffset? printedAt,
        Guid? operatorTenantUserId,
        Guid? printerDeviceId,
        string? errorCode,
        string? errorMessage,
        string? printResultJson,
        DateTimeOffset now)
    {
        return new ReceiptPrintLog
        {
            Id = id,
            TenantId = tenantId,
            ReceiptId = receiptId,
            AttemptNumber = attemptNumber,
            PrintedCopyType = printedCopyType.Trim().ToUpperInvariant(),
            PrintStatus = printStatus.Trim().ToUpperInvariant(),
            PrintedAt = printedAt,
            OperatorTenantUserId = operatorTenantUserId,
            PrinterDeviceId = printerDeviceId,
            ErrorCode = errorCode?.Trim(),
            ErrorMessage = errorMessage?.Trim(),
            PrintResultJson = printResultJson,
            CreatedAt = now
        };
    }
}

