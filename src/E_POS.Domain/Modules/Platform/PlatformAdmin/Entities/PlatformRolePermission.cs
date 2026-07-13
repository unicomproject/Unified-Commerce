using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformRolePermission : AuditableEntity
{
    public string? Description { get; protected set; }
    public Guid PlatformPermissionId { get; protected set; }
    public Guid PlatformRoleId { get; protected set; }
    public DateTimeOffset? GrantedAt { get; protected set; }
    public Guid? GrantedByPlatformUserId { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokedReason { get; protected set; }

    public static PlatformRolePermission Create(
        Guid id,
        Guid platformRoleId,
        Guid platformPermissionId,
        string? description,
        DateTimeOffset now,
        Guid? grantedByPlatformUserId = null)
    {
        return new PlatformRolePermission
        {
            Id = id,
            PlatformRoleId = platformRoleId,
            PlatformPermissionId = platformPermissionId,
            Description = description,
            GrantedAt = now,
            GrantedByPlatformUserId = grantedByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(Guid? revokedByPlatformUserId, string? revokedReason, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedByPlatformUserId = revokedByPlatformUserId;
        RevokedReason = revokedReason;
        UpdatedAt = now;
    }
}
