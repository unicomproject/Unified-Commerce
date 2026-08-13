using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class UserInvite : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string InvitedEmail { get; protected set; } = string.Empty;
    public string NormalizedInvitedEmail { get; protected set; } = string.Empty;
    public string? InvitedPhone { get; protected set; }
    public string? NormalizedInvitedPhone { get; protected set; }
    public Guid? AcceptedTenantUserId { get; protected set; }
    public Guid? TenantUserId { get; protected set; }
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
        DateTimeOffset now,
        Guid? tenantUserId = null)
    {
        return new UserInvite
        {
            Id = id,
            TenantId = tenantId,
            InvitedEmail = invitedEmail,
            NormalizedInvitedEmail = normalizedInvitedEmail,
            InitialRoleId = initialRoleId,
            TenantUserId = tenantUserId,
            InvitedByPlatformUserId = invitedByPlatformUserId,
            InviteTokenHash = inviteTokenHash,
            InviteStatus = UserInviteConstants.StatusPending,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Cancel(DateTimeOffset now)
    {
        if (InviteStatus == UserInviteConstants.StatusAccepted) return;
        InviteStatus = UserInviteConstants.StatusCancelled;
        CancelledAt = now;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (InviteStatus == UserInviteConstants.StatusAccepted)
            throw new InvalidOperationException("An accepted invitation cannot be revoked.");
        if (InviteStatus == UserInviteConstants.StatusRevoked) return;
        InviteStatus = UserInviteConstants.StatusRevoked;
        CancelledAt = now;
        UpdatedAt = now;
    }

    public bool Targets(Guid tenantUserId) => TenantUserId == tenantUserId;

    public void MarkSent(DateTimeOffset now)
    {
        InviteStatus = UserInviteConstants.StatusSent;
        SentAt ??= now;
        LastSentAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Atomically consume a usable invitation. Call only after password/user activation is prepared
    /// in the same transaction. Rejects cancelled, accepted, or expired invites.
    /// </summary>
    public void MarkAccepted(Guid acceptedTenantUserId, DateTimeOffset now)
    {
        if (AcceptedAt.HasValue || InviteStatus == UserInviteConstants.StatusAccepted)
        {
            throw new InvalidOperationException("Invitation has already been accepted.");
        }

        if (CancelledAt.HasValue ||
            InviteStatus is UserInviteConstants.StatusCancelled or UserInviteConstants.StatusRevoked)
        {
            throw new InvalidOperationException("Invitation has been cancelled.");
        }

        if (InviteStatus is not (UserInviteConstants.StatusPending or UserInviteConstants.StatusSent))
        {
            throw new InvalidOperationException("Invitation is not in an acceptable state.");
        }

        if (ExpiresAt <= now)
        {
            throw new InvalidOperationException("Invitation has expired.");
        }

        InviteStatus = UserInviteConstants.StatusAccepted;
        AcceptedAt = now;
        AcceptedTenantUserId = acceptedTenantUserId;
        UpdatedAt = now;
    }

    public bool IsUsableAt(DateTimeOffset now) =>
        !AcceptedAt.HasValue &&
        !CancelledAt.HasValue &&
        InviteStatus is UserInviteConstants.StatusPending or UserInviteConstants.StatusSent &&
        ExpiresAt > now;
}

