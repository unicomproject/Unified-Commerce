using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

/// <summary>
/// Platform-level option template master. No tenant_id — shared globally.
/// </summary>
public class ProductOptionTemplate : AuditableEntity
{
    public string TemplateCode { get; protected set; } = string.Empty;
    public string TemplateName { get; protected set; } = string.Empty;
    public string OptionType { get; protected set; } = string.Empty;
    public string InputType { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static ProductOptionTemplate Create(
        Guid id,
        string templateCode,
        string templateName,
        string optionType,
        string inputType,
        int sortOrder,
        string status,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new ProductOptionTemplate
        {
            Id = id,
            TemplateCode = templateCode.Trim(),
            TemplateName = templateName.Trim(),
            OptionType = optionType.Trim().ToUpperInvariant(),
            InputType = inputType.Trim().ToUpperInvariant(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
