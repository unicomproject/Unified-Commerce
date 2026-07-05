using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerConsent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerId { get; protected set; }
    public string ConsentType { get; protected set; } = string.Empty;
    public Guid? SalesChannelId { get; protected set; }
    public string? PolicyVersion { get; protected set; }
    public string ConsentStatus { get; protected set; } = string.Empty;
    public string ConsentSource { get; protected set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; protected set; }
    public DateTimeOffset? WithdrawnAt { get; protected set; }
    public IPAddress? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
}
