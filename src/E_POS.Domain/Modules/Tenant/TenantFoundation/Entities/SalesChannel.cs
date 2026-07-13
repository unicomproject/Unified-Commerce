using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class SalesChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PlatformSalesChannelId { get; protected set; }
    public string CustomName { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }

    public static SalesChannel Create(
        Guid id,
        Guid tenantId,
        Guid platformSalesChannelId,
        string customName,
        string status,
        int sortOrder,
        DateTimeOffset now)
    {
        return new SalesChannel
        {
            Id = id,
            TenantId = tenantId,
            PlatformSalesChannelId = platformSalesChannelId,
            CustomName = customName.Trim(),
            Status = status.Trim().ToUpperInvariant(),
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string customName,
        string status,
        int sortOrder,
        DateTimeOffset now)
    {
        CustomName = customName.Trim();
        Status = status.Trim().ToUpperInvariant();
        SortOrder = sortOrder;
        UpdatedAt = now;
    }
}
