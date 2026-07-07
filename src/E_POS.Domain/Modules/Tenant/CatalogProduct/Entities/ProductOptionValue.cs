using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductOptionValue : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductOptionId { get; protected set; }
    public Guid? SourceOptionTemplateValueId { get; protected set; }
    public string ValueCode { get; protected set; } = string.Empty;
    public string ValueName { get; protected set; } = string.Empty;
    public string? DisplayName { get; protected set; }
    public string? ColorHex { get; protected set; }
    public string? ImageUrl { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductOptionValue Create(
        Guid id,
        Guid tenantId,
        Guid productOptionId,
        Guid? sourceOptionTemplateValueId,
        string valueCode,
        string valueName,
        string? displayName,
        string? colorHex,
        string? imageUrl,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductOptionValue
        {
            Id = id,
            TenantId = tenantId,
            ProductOptionId = productOptionId,
            SourceOptionTemplateValueId = sourceOptionTemplateValueId,
            ValueCode = valueCode.Trim(),
            ValueName = valueName.Trim(),
            DisplayName = displayName?.Trim(),
            ColorHex = colorHex?.Trim(),
            ImageUrl = imageUrl?.Trim(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

