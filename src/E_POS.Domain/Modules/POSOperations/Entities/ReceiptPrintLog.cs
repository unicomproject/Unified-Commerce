using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class ReceiptPrintLog : AuditableEntity
{
    public int AttemptNumber { get; protected set; }
    public Guid ReceiptId { get; protected set; }
}
