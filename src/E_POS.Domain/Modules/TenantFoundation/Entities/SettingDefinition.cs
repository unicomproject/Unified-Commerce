using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class SettingDefinition : AuditableEntity
{
    public string SettingKey { get; protected set; } = string.Empty;
    public string ValueType { get; protected set; } = string.Empty;
}
