using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed class PlatformTenantCreateWriteModel
{
    public required E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant Tenant { get; init; }

    public TenantProfile? Profile { get; init; }

    public TenantAddress? Address { get; init; }

    public TenantDomain? Domain { get; init; }

    public required TenantSubscription Subscription { get; init; }

    public IReadOnlyList<TenantFeatureEntitlement> Entitlements { get; init; } = [];

    public IReadOnlyList<TenantSubscriptionAddon> SubscriptionAddons { get; init; } = [];

    public TenantRole? TenantAdminRole { get; init; }

    public IReadOnlyList<TenantRolePermission> TenantAdminRolePermissions { get; init; } = [];

    public TenantUser? TenantAdminUser { get; init; }

    public TenantUserRole? TenantAdminUserRole { get; init; }

    public UserInvite? TenantAdminInvite { get; init; }

    public SubscriptionInvoice? DraftInvoice { get; init; }

    public IReadOnlyList<SubscriptionInvoiceLine> DraftInvoiceLines { get; init; } = [];

    public SubscriptionPaymentTransaction? ManualPayment { get; init; }

    public SubscriptionPaymentLink? ManualPaymentAccess { get; init; }

    public SubscriptionPaymentReview? ManualPaymentCreatedHistory { get; init; }

    public PlatformTenantOnboardingFinalizeContext? OnboardingFinalizeContext { get; init; }

    public PlatformTenantOnboardingOperation? OnboardingOperation { get; init; }

    public IReadOnlyList<TenantContact> OnboardingContacts { get; init; } = [];

    public IReadOnlyList<IntegrationOutboxMessage> OnboardingOutboxMessages { get; init; } = [];
}


