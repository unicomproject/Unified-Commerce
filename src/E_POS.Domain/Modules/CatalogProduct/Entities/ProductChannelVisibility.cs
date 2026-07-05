using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductChannelVisibility : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public bool IsVisible { get; protected set; } = true;
    public bool IsOrderable { get; protected set; } = true;
    public DateTimeOffset? AvailableFrom { get; protected set; }
    public DateTimeOffset? AvailableUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductChannelVisibility Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        Guid salesChannelId,
        bool isVisible,
        bool isOrderable,
        DateTimeOffset? availableFrom,
        DateTimeOffset? availableUntil,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductChannelVisibility
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            SalesChannelId = salesChannelId,
            IsVisible = isVisible,
            IsOrderable = isOrderable,
            AvailableFrom = availableFrom,
            AvailableUntil = availableUntil,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
