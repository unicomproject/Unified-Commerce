using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

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
}
