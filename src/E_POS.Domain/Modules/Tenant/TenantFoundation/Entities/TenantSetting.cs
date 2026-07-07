using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class TenantSetting : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SettingDefinitionId { get; protected set; }
}

