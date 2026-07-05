using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductOptionTemplateValue : AuditableEntity
{
    public Guid OptionTemplateId { get; protected set; }
    public string ValueCode { get; protected set; } = string.Empty;
    public string ValueName { get; protected set; } = string.Empty;
    public string? DisplayName { get; protected set; }
    public string? ColorHex { get; protected set; }
    public string? ImageUrl { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static ProductOptionTemplateValue Create(
        Guid id,
        Guid optionTemplateId,
        string valueCode,
        string valueName,
        string? displayName,
        string? colorHex,
        string? imageUrl,
        int sortOrder,
        string status,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new ProductOptionTemplateValue
        {
            Id = id,
            OptionTemplateId = optionTemplateId,
            ValueCode = valueCode.Trim(),
            ValueName = valueName.Trim(),
            DisplayName = displayName?.Trim(),
            ColorHex = colorHex?.Trim(),
            ImageUrl = imageUrl?.Trim(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
