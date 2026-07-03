using E_POS.Domain.Modules.AccessControl.Entities;
using E_POS.Domain.Modules.AuthSecurity.Entities;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed class PlatformTenantCreateWriteModel
{
    public required Tenant Tenant { get; init; }

    public TenantProfile? Profile { get; init; }

    public TenantAddress? Address { get; init; }

    public required TenantSubscription Subscription { get; init; }

    public IReadOnlyList<TenantFeatureEntitlement> Entitlements { get; init; } = [];

    public IReadOnlyList<TenantSubscriptionAddon> SubscriptionAddons { get; init; } = [];

    public TenantRole? TenantAdminRole { get; init; }

    public IReadOnlyList<TenantRolePermission> TenantAdminRolePermissions { get; init; } = [];

    public TenantUser? TenantAdminUser { get; init; }

    public TenantUserRole? TenantAdminUserRole { get; init; }

    public UserInvite? TenantAdminInvite { get; init; }

    public SubscriptionInvoice? DraftInvoice { get; init; }
}
