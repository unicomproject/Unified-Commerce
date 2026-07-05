using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class BusinessTypeOptionTemplate : AuditableEntity
{
    public Guid BusinessTypeId { get; protected set; }
    public Guid OptionTemplateId { get; protected set; }
    public bool IsDefaultTemplate { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static BusinessTypeOptionTemplate Create(
        Guid id,
        Guid businessTypeId,
        Guid optionTemplateId,
        bool isDefaultTemplate,
        int sortOrder,
        string status,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new BusinessTypeOptionTemplate
        {
            Id = id,
            BusinessTypeId = businessTypeId,
            OptionTemplateId = optionTemplateId,
            IsDefaultTemplate = isDefaultTemplate,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
