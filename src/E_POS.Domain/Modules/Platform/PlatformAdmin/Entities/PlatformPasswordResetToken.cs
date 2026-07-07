using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformPasswordResetToken : AuditableEntity
{
    public Guid? PlatformUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string TokenHash { get; protected set; } = string.Empty;
}

