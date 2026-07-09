using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class FeatureLimitDefinition : AuditableEntity
{
    public string LimitCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public decimal? DefaultLimitValue { get; protected set; }
    public Guid PlatformFeatureId { get; protected set; }
    public string LimitKey { get; protected set; } = string.Empty;
    public string LimitName { get; protected set; } = string.Empty;
    public string ValueType { get; protected set; } = SubscriptionCatalogConstants.LimitValueType.Integer;
    public string? UnitCode { get; protected set; }
    public bool IsHardLimit { get; protected set; } = true;
    public string Status { get; protected set; } = SubscriptionCatalogConstants.RecordStatus.Active;

    public static FeatureLimitDefinition Create(
        Guid id,
        Guid platformFeatureId,
        string limitCode,
        string name,
        decimal? defaultLimitValue,
        DateTimeOffset now,
        string valueType = SubscriptionCatalogConstants.LimitValueType.Integer,
        string? unitCode = null,
        bool isHardLimit = true)
    {
        return new FeatureLimitDefinition
        {
            Id = id,
            PlatformFeatureId = platformFeatureId,
            LimitCode = limitCode,
            Name = name,
            DefaultLimitValue = defaultLimitValue,
            LimitKey = limitCode,
            LimitName = name,
            ValueType = valueType,
            UnitCode = unitCode,
            IsHardLimit = isHardLimit,
            Status = SubscriptionCatalogConstants.RecordStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
