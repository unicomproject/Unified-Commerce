using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

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

    protected CustomerPasswordResetToken() { }

    public static CustomerPasswordResetToken Create(
        Guid id,
        Guid tenantId,
        Guid customerAuthAccountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        IPAddress? requestIpAddress,
        string? requestUserAgent,
        Guid? verifiedOtpId = null)
    {
        return new CustomerPasswordResetToken
        {
            Id = id,
            TenantId = tenantId,
            CustomerAuthAccountId = customerAuthAccountId,
            TokenHash = tokenHash,
            VerifiedOtpId = verifiedOtpId,
            Status = "ACTIVE",
            ExpiresAt = expiresAt,
            RequestIpAddress = requestIpAddress,
            RequestUserAgent = string.IsNullOrWhiteSpace(requestUserAgent) ? null : requestUserAgent.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsActive(DateTimeOffset now) =>
        string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
        UsedAt is null &&
        RevokedAt is null &&
        ExpiresAt > now;

    public void Use(DateTimeOffset now)
    {
        Status = "USED";
        UsedAt = now;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTimeOffset now)
    {
        Status = "EXPIRED";
        RevokedAt = now;
        RevokedReason = "EXPIRED";
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        Status = "REVOKED";
        RevokedAt = now;
        RevokedReason = string.IsNullOrWhiteSpace(reason) ? "REVOKED" : reason.Trim();
        UpdatedAt = now;
    }
}