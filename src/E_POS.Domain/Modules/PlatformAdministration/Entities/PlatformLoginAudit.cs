using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

public class PlatformLoginAudit : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string LoginResult { get; protected set; } = string.Empty;
}
