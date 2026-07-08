using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantProfile : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? BusinessTypeId { get; protected set; }
    public string LegalName { get; protected set; } = string.Empty;
    public string? TradingName { get; protected set; }
    public string? PrimaryContactName { get; protected set; }
    public string? PrimaryEmail { get; protected set; }
    public string? PrimaryPhone { get; protected set; }
    public string? WebsiteUrl { get; protected set; }
    public string? LogoUrl { get; protected set; }
    public string? Description { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static TenantProfile Create(
        Guid id,
        Guid tenantId,
        Guid? businessTypeId,
        string legalName,
        string? tradingName,
        string? primaryContactName,
        string? primaryEmail,
        string? primaryPhone,
        string? websiteUrl,
        string? logoUrl,
        string? description,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new TenantProfile
        {
            Id = id,
            TenantId = tenantId,
            BusinessTypeId = businessTypeId,
            LegalName = legalName.Trim(),
            TradingName = tradingName?.Trim(),
            PrimaryContactName = primaryContactName?.Trim(),
            PrimaryEmail = primaryEmail?.Trim(),
            PrimaryPhone = primaryPhone?.Trim(),
            WebsiteUrl = websiteUrl?.Trim(),
            LogoUrl = logoUrl?.Trim(),
            Description = description?.Trim(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
