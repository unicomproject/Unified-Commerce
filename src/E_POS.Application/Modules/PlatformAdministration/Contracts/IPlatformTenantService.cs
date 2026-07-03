using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.SubscriptionBilling.Entities;
using E_POS.Domain.Modules.TenantFoundation.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformTenantRepository
{
    Task<PlatformTenantListResponse> GetTenantsAsync(
        PlatformTenantListQuery query,
        CancellationToken cancellationToken);

    Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);

    Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken);

    Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<PlatformTenantEntitlementOptionsResponse?> GetEntitlementOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken);

    Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddTenantWithSubscriptionAndEntitlementsAsync(
        Tenant tenant,
        TenantSubscription subscription,
        IReadOnlyList<Guid> enabledFeatureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken);

    Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task UpdateTenantSubscriptionAsync(
        TenantSubscription subscription,
        CancellationToken cancellationToken);

    Task ReplaceTenantEntitlementsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> enabledFeatureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
        IReadOnlyList<Guid>? featureIds,
        IReadOnlyList<string>? featureCodes,
        CancellationToken cancellationToken);

    Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken cancellationToken);

    Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken);

    Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetTenantAdminBootstrapPermissionIdsAsync(CancellationToken cancellationToken);
}

public interface IPlatformTenantService
{
    Task<ApplicationResult<PlatformTenantListResponse>> GetTenantsAsync(
        PlatformTenantListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantSummaryResponse>> GetSummaryAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantFilterOptionsResponse>> GetFilterOptionsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantDetailResponse>> GetTenantDetailAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantDetailResponse>> CreateTenantAsync(
        CreatePlatformTenantRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantDetailResponse>> UpdateTenantAsync(
        Guid tenantId,
        UpdatePlatformTenantRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantDetailResponse>> ActivateTenantAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantDetailResponse>> SuspendTenantAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantDetailResponse>> UpdateEntitlementsAsync(
        Guid tenantId,
        UpdatePlatformTenantEntitlementsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantEntitlementOptionsResponse>> GetEntitlementOptionsAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantCreateOptionsResponse>> GetCreateOptionsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}
