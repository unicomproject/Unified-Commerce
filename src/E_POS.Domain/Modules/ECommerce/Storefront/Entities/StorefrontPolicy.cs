using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Storefront.Entities;

public sealed class StorefrontPolicy : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid SalesChannelId { get; private set; }
    public string PolicyType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? CreatedByTenantUserId { get; private set; }
    public Guid? UpdatedByTenantUserId { get; private set; }

    private StorefrontPolicy()
    {
    }

    public static StorefrontPolicy Create(
        Guid id,
        Guid tenantId,
        Guid salesChannelId,
        string policyType,
        string title,
        string content,
        string version,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new StorefrontPolicy
        {
            Id = id,
            TenantId = tenantId,
            SalesChannelId = salesChannelId,
            PolicyType = Normalize(policyType),
            Title = title.Trim(),
            Content = content.Trim(),
            Version = version.Trim(),
            Status = Normalize(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(string title, string content, string version, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Title = title.Trim();
        Content = content.Trim();
        Version = version.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void Publish(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "PUBLISHED";
        EffectiveFrom ??= now;
        PublishedAt = now;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void Archive(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "ARCHIVED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
