using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerAuthSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid CustomerAuthAccountId { get; protected set; }
    public string SessionTokenHash { get; protected set; } = string.Empty;
    public IPAddress? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
    public string? DeviceName { get; protected set; }
    public DateTimeOffset? LastActivityAt { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public string? RevokedReason { get; protected set; }
}
