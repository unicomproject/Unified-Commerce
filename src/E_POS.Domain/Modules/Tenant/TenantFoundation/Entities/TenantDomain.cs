using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantDomain : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public string DomainType { get; protected set; } = string.Empty;
    public string DomainName { get; protected set; } = string.Empty;
    public bool IsPrimary { get; protected set; }
    public string VerificationStatus { get; protected set; } = string.Empty;
    public string? VerificationTokenHash { get; protected set; }
    public DateTimeOffset? VerifiedAt { get; protected set; }
    public string SslStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? SslIssuedAt { get; protected set; }
    public DateTimeOffset? SslExpiresAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static TenantDomain Create(
        Guid id,
        Guid tenantId,
        Guid? salesChannelId,
        string domainType,
        string domainName,
        bool isPrimary,
        string verificationStatus,
        string? verificationTokenHash,
        DateTimeOffset? verifiedAt,
        string sslStatus,
        DateTimeOffset? sslIssuedAt,
        DateTimeOffset? sslExpiresAt,
        string status,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new TenantDomain
        {
            Id = id,
            TenantId = tenantId,
            SalesChannelId = salesChannelId,
            DomainType = domainType.Trim(),
            DomainName = domainName.Trim(),
            IsPrimary = isPrimary,
            VerificationStatus = verificationStatus.Trim(),
            VerificationTokenHash = verificationTokenHash?.Trim(),
            VerifiedAt = verifiedAt,
            SslStatus = sslStatus.Trim(),
            SslIssuedAt = sslIssuedAt,
            SslExpiresAt = sslExpiresAt,
            Status = status.Trim(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

