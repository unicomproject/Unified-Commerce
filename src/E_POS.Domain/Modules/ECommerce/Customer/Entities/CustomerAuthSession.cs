using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

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

    protected CustomerAuthSession() { }

    public static CustomerAuthSession Create(
        Guid id,
        Guid tenantId,
        Guid customerAuthAccountId,
        string sessionTokenHash,
        IPAddress? ipAddress,
        string? userAgent,
        string? deviceName,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new CustomerAuthSession
        {
            Id = id,
            TenantId = tenantId,
            CustomerAuthAccountId = customerAuthAccountId,
            SessionTokenHash = sessionTokenHash,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Trim(),
            DeviceName = deviceName?.Trim(),
            Status = "ACTIVE",
            LastActivityAt = now,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        if (RevokedAt.HasValue) return;
        Status = "REVOKED";
        RevokedAt = now;
        RevokedReason = reason.Trim();
        UpdatedAt = now;
    }

    public void Extend(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (RevokedAt.HasValue ||
            !string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A revoked session cannot be extended.");
        }

        ExpiresAt = expiresAt;
        LastActivityAt = now;
        UpdatedAt = now;
    }
}
