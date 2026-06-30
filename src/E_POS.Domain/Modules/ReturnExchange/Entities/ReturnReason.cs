using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class ReturnReason : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReasonCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string AppliesTo { get; protected set; } = string.Empty;
}
