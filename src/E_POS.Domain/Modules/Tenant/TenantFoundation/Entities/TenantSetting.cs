using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantSetting : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SettingDefinitionId { get; protected set; }
    public string SettingValue { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static TenantSetting Create(
        Guid id,
        Guid tenantId,
        Guid settingDefinitionId,
        string settingValue,
        Guid? createdByPlatformUserId,
        DateTimeOffset now)
    {
        return new TenantSetting
        {
            Id = id,
            TenantId = tenantId,
            SettingDefinitionId = settingDefinitionId,
            SettingValue = settingValue.Trim(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

