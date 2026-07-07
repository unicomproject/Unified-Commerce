using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class UserInvite : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string InvitedEmail { get; protected set; } = string.Empty;
    public string NormalizedInvitedEmail { get; protected set; } = string.Empty;
    public string? InvitedPhone { get; protected set; }
    public string? NormalizedInvitedPhone { get; protected set; }
    public Guid? AcceptedTenantUserId { get; protected set; }
    public Guid? InitialRoleId { get; protected set; }
    public Guid? InitialOutletId { get; protected set; }
    public string InviteTokenHash { get; protected set; } = string.Empty;
    public string InviteStatus { get; protected set; } = string.Empty;
    public Guid? InvitedByTenantUserId { get; protected set; }
    public Guid? InvitedByPlatformUserId { get; protected set; }
    public DateTimeOffset? SentAt { get; protected set; }
    public DateTimeOffset? LastSentAt { get; protected set; }
    public int ResendCount { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? AcceptedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }

    public static UserInvite CreatePending(
        Guid id,
        Guid tenantId,
        string invitedEmail,
        string normalizedInvitedEmail,
        Guid? initialRoleId,
        Guid? invitedByPlatformUserId,
        string inviteTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new UserInvite
        {
            Id = id,
            TenantId = tenantId,
            InvitedEmail = invitedEmail,
            NormalizedInvitedEmail = normalizedInvitedEmail,
            InitialRoleId = initialRoleId,
            InvitedByPlatformUserId = invitedByPlatformUserId,
            InviteTokenHash = inviteTokenHash,
            InviteStatus = "PENDING",
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

