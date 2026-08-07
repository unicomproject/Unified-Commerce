using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantAuth.Contracts;

public interface ITenantAdminInvitationAcceptanceService
{
    Task<ValidateTenantAdminSetupTokenResponse> ValidateSetupTokenAsync(
        string rawToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SetupTenantAdminPasswordResponse>> SetupPasswordAsync(
        SetupTenantAdminPasswordRequest request,
        CancellationToken cancellationToken);
}

public interface ITenantAdminInvitationAcceptanceRepository
{
    Task<TenantAdminInvitationAcceptanceSnapshot?> GetByTokenHashForReadAsync(
        string inviteTokenHash,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens a transaction, locks the invite row (FOR UPDATE), runs <paramref name="action"/>,
    /// then SaveChanges + Commit. Returns null claim to the action when the invite cannot be loaded.
    /// </summary>
    Task<TResult> ExecuteClaimAsync<TResult>(
        string inviteTokenHash,
        Func<TenantAdminInvitationAcceptanceClaim?, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken);
}

public sealed class TenantAdminInvitationAcceptanceSnapshot
{
    public required Guid InviteId { get; init; }
    public required Guid TenantId { get; init; }
    public required string InviteStatus { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required DateTimeOffset? AcceptedAt { get; init; }
    public required DateTimeOffset? CancelledAt { get; init; }
    public required string InvitedEmail { get; init; }
    public required string NormalizedInvitedEmail { get; init; }
    public required string TenantStatus { get; init; }
    public required string TenantDisplayName { get; init; }
    public required Guid? TenantUserId { get; init; }
    public required string? TenantUserStatus { get; init; }
}

public sealed class TenantAdminInvitationAcceptanceClaim
{
    public required Domain.Modules.Tenant.TenantAuth.Entities.UserInvite Invite { get; init; }
    public required Domain.Modules.Tenant.AccessControl.Entities.TenantUser User { get; init; }
    public required Domain.Modules.Tenant.TenantFoundation.Entities.Tenant Tenant { get; init; }
    public Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantOnboardingOperation? Operation { get; init; }
    public required IReadOnlyList<Domain.Modules.Tenant.TenantAuth.Entities.UserInvite> SiblingOpenInvites { get; init; }
}
