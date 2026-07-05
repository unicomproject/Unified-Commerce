using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class TillCashMovement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public string MovementType { get; protected set; } = string.Empty;
    public decimal Amount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string? Reason { get; protected set; }
    public string? ReferenceNumber { get; protected set; }
    public Guid PerformedByTenantUserId { get; protected set; }
    public DateTimeOffset PerformedAt { get; protected set; }
}
