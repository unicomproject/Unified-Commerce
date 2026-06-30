using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class ReceiptTemplateVersion : AuditableEntity
{
    public Guid ReceiptTemplateId { get; protected set; }
    public int VersionNumber { get; protected set; }
}
