using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerPasswordResetToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerAuthAccountId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public Guid? VerifiedOtpId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public string? RevokedReason { get; protected set; }
    public IPAddress? RequestIpAddress { get; protected set; }
    public string? RequestUserAgent { get; protected set; }
}
