using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class ReturnInspection : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public decimal InspectedQuantity { get; protected set; }
    public string InspectionNumber { get; protected set; } = string.Empty;
    public Guid SalesReturnLineId { get; protected set; }
}
