using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class TenantUser : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string NormalizedEmail { get; protected set; } = string.Empty;
    public string NormalizedPhone { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
}
