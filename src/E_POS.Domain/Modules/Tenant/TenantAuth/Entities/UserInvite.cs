using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class UserInvite : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TenantUserId { get; protected set; }
    public string InviteStatus { get; protected set; } = string.Empty;
    public string InviteTokenHash { get; protected set; } = string.Empty;

    public static UserInvite CreatePending(
        Guid id,
        Guid tenantId,
        Guid tenantUserId,
        DateTimeOffset now)
    {
        return new UserInvite
        {
            Id = id,
            TenantId = tenantId,
            TenantUserId = tenantUserId,
            InviteStatus = UserInviteConstants.StatusPending,
            InviteTokenHash = UserInviteConstants.PendingInviteTokenHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

