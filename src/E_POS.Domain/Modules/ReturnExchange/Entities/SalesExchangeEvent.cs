using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesExchangeEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesExchangeId { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? EventNotes { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
}
