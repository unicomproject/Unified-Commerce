using E_POS.Domain.Common.Entities;
using System.Net;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class TillSessionEvent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public string EventType { get; protected set; } = string.Empty;
    public DateTimeOffset EventAt { get; protected set; }
    public Guid? EventByTenantUserId { get; protected set; }
    public decimal? Amount { get; protected set; }
    public string? CurrencyCode { get; protected set; }
    public string? ReferenceType { get; protected set; }
    public Guid? ReferenceId { get; protected set; }
    public string? EventPayloadJson { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public IPAddress? IpAddress { get; protected set; }
    public string? Notes { get; protected set; }
}
