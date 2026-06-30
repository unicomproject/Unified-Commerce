using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class DocumentNumberSequence : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string DocumentType { get; protected set; } = string.Empty;
    public string DocumentSubtype { get; protected set; } = string.Empty;
    public int CurrentValue { get; protected set; }
    public int PaddingLength { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
}
