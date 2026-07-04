using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class ReturnInspection : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesReturnLineId { get; protected set; }
    public Guid? InspectedByTenantUserId { get; protected set; }
    public Guid? InventoryLocationId { get; protected set; }
    public string InspectionStatus { get; protected set; } = string.Empty;
    public string? ConditionCode { get; protected set; }
    public string RestockDecision { get; protected set; } = string.Empty;
    public decimal? RestockQuantity { get; protected set; }
    public decimal? RejectQuantity { get; protected set; }
    public string? InspectionNotes { get; protected set; }
    public DateTimeOffset? InspectedAt { get; protected set; }
}
