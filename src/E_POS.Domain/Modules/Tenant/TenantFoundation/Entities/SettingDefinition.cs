using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class SettingDefinition : AuditableEntity
{
    public string SettingKey { get; protected set; } = string.Empty;
    public string DisplayName { get; protected set; } = string.Empty;
    public string ValueType { get; protected set; } = string.Empty;
    public string? DefaultValue { get; protected set; }
    public string? Description { get; protected set; }
    public bool IsTenantEditable { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static SettingDefinition Create(
        Guid id,
        string settingKey,
        string displayName,
        string valueType,
        string? defaultValue,
        string? description,
        bool isTenantEditable,
        string status,
        DateTimeOffset now)
    {
        return new SettingDefinition
        {
            Id = id,
            SettingKey = settingKey.Trim(),
            DisplayName = displayName.Trim(),
            ValueType = valueType.Trim(),
            DefaultValue = defaultValue?.Trim(),
            Description = description?.Trim(),
            IsTenantEditable = isTenantEditable,
            Status = status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

