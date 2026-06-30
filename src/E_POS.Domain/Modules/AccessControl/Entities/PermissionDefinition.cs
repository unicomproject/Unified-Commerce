using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class PermissionDefinition : AuditableEntity
{
    public string PermissionCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
}
