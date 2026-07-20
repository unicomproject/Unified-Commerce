using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnInspectionCondition : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ConditionCode { get; protected set; } = string.Empty;
    public string DisplayName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string StatusCategory { get; protected set; } = string.Empty;
    public bool IsResellable { get; protected set; }
    public string RefundImpact { get; protected set; } = string.Empty;
    public bool RequiresNotes { get; protected set; }
    public bool RequiresPhoto { get; protected set; }
    public bool RequiresApproval { get; protected set; }
    public bool IsActive { get; protected set; }
    public int SortOrder { get; protected set; }
}
