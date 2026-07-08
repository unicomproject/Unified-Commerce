using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class SalesReturnEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesReturnId { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string? EventNotes { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
}

