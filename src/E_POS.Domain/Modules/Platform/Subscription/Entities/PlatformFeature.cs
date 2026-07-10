using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class PlatformFeature : AuditableEntity
{
    public string FeatureCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformModuleId { get; protected set; }
    public int SortOrder { get; protected set; }
    public string FeatureKey { get; protected set; } = string.Empty;
    public string FeatureName { get; protected set; } = string.Empty;
    public bool IsCoreFeature { get; protected set; }

    public static PlatformFeature Create(
        Guid id,
        Guid platformModuleId,
        string featureCode,
        string name,
        string status,
        DateTimeOffset createdAt,
        int sortOrder = 0,
        string? description = null,
        bool isCoreFeature = false)
    {
        return new PlatformFeature
        {
            Id = id,
            PlatformModuleId = platformModuleId,
            FeatureCode = featureCode,
            Name = name,
            Description = description,
            Status = status,
            SortOrder = sortOrder,
            FeatureKey = featureCode,
            FeatureName = name,
            IsCoreFeature = isCoreFeature,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
