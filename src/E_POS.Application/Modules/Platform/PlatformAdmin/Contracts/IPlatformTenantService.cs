using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

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

    Task<E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddTenantWithSubscriptionAndEntitlementsAsync(
        E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant tenant,
        TenantSubscription subscription,
        IReadOnlyList<Guid> enabledFeatureIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task UpdateTenantAsync(E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant tenant, CancellationToken cancellationToken);

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
        Guid? actorPlatformUserId,
        string? revokedReason,
        CancellationToken cancellationToken);

    Task ReplaceTenantEntitlementsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> enabledFeatureIds,
        DateTimeOffset now,
        Guid? actorPlatformUserId,
        string? revokedReason,
        string sourceType,
        string? overrideReason,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveUntil,
        CancellationToken cancellationToken);

    Task RestoreTenantPlanEntitlementsAsync(
        Guid tenantId,
        Guid subscriptionPlanId,
        DateTimeOffset now,
        Guid? actorPlatformUserId,
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

    Task MarkTenantAdminInviteSentAsync(Guid inviteId, DateTimeOffset sentAt, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Returns active permission definition IDs keyed by permission code for the requested codes.
    /// Missing/inactive codes are omitted from the dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken);

    Task<Guid?> GetActiveBusinessTypeIdByCodeAsync(string businessCode, CancellationToken cancellationToken);

    Task<TenantProfile?> GetTenantProfileEntityByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task UpsertTenantProfileAsync(TenantProfile profile, CancellationToken cancellationToken);

    /// <summary>
    /// True when the tenant has at least one subscription invoice with authoritative PAID status.
    /// Used as Release 1 payment-verification evidence for paid activation.
    /// </summary>
    Task<bool> HasVerifiedPaidInvoiceAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<PlatformTenantActivationRuntimeResult> ActivateTenantRuntimeAsync(
        Guid tenantId,
        Guid actorPlatformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PlatformTenantAuditLogListResponse> GetTenantAuditLogsAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddAuditLogAsync(
        Guid tenantId,
        Guid? platformUserId,
        string action,
        string summary,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task AddAuditLogAsync(
        Guid tenantId,
        Guid? platformUserId,
        string action,
        string summary,
        string? reason,
        DateTimeOffset now,
        string? entityType,
        Guid? entityId,
        object? before,
        object? after,
        string? correlationId,
        CancellationToken cancellationToken);
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

    Task<ApplicationResult<PlatformTenantDetailResponse>> ReactivateTenantAsync(
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

    Task<ApplicationResult<PlatformTenantDetailResponse>> RestoreEntitlementsToPlanAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantEntitlementOptionsResponse>> GetEntitlementOptionsAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantCreateOptionsResponse>> GetCreateOptionsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformTenantAuditLogListResponse>> GetTenantAuditLogsAsync(
        Guid tenantId,
        Guid platformUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
