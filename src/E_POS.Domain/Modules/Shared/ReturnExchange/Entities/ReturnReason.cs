using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnReason : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReasonCode { get; protected set; } = string.Empty;
    public string ReasonName { get; protected set; } = string.Empty;
    public string AppliesTo { get; protected set; } = string.Empty;
    public bool RequiresInspection { get; protected set; }
    public bool IsActive { get; protected set; }
    public int SortOrder { get; protected set; }
}

