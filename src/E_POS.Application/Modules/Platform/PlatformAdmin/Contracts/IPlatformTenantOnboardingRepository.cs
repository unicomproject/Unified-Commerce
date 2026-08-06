using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public sealed class TenantOnboardingConcurrencyException : Exception
{
    public TenantOnboardingConcurrencyException(Exception inner) : base("Tenant onboarding resource changed concurrently.", inner) { }
}

public sealed class TenantOnboardingAlreadyFinalizedException : Exception
{
    public TenantOnboardingAlreadyFinalizedException(bool sameRequest)
        : base("Tenant onboarding draft was already finalized.") => SameRequest = sameRequest;

    public bool SameRequest { get; }
}

public interface IPlatformTenantOnboardingRepository
{
    Task AddDraftAsync(PlatformTenantOnboardingDraft draft, CancellationToken cancellationToken);
    Task<PlatformTenantOnboardingDraft?> GetDraftAsync(Guid id, CancellationToken cancellationToken, bool tracking = true);
    Task<IReadOnlyList<PlatformTenantOnboardingDraft>> ListDraftsAsync(Guid actorId, bool includeAll, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PlatformTenantOnboardingOperation?> GetOperationByDraftAsync(Guid draftId, CancellationToken cancellationToken);
    Task<PlatformTenantOnboardingOperation?> GetOperationAsync(Guid operationId, CancellationToken cancellationToken);
    Task<PlatformTenantOnboardingOperation?> GetOperationByTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddCompletionAsync(PlatformTenantOnboardingOperation operation, IReadOnlyList<TenantContact> contacts,
        IReadOnlyList<IntegrationOutboxMessage> outboxMessages, CancellationToken cancellationToken);
    Task<bool> RetryOperationAsync(Guid operationId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<TenantInvitationResendResult> ResendInvitationAsync(Guid tenantId, string idempotencyKeyHash,
        string requestHash, Guid actorId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IPlatformTenantOnboardingService
{
    Task<ApplicationResult<TenantOnboardingDraftResponse>> CreateDraftAsync(CreateTenantOnboardingDraftRequest request, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingDraftListResponse>> ListDraftsAsync(Guid actorId, bool includeAll, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingDraftResponse>> GetDraftAsync(Guid draftId, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingDraftResponse>> UpdateDraftAsync(Guid draftId, UpdateTenantOnboardingDraftRequest request, long expectedVersion, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult> DiscardDraftAsync(Guid draftId, long expectedVersion, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingValidationResponse>> ValidateDraftAsync(Guid draftId, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingReceiptResponse>> FinalizeAsync(Guid draftId, FinalizeTenantOnboardingRequest request, long expectedVersion, string idempotencyKey, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingOperationResponse>> GetOperationAsync(Guid operationId, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingOperationResponse>> RetryOperationAsync(Guid operationId, Guid actorId, CancellationToken cancellationToken);
    Task<ApplicationResult<TenantOnboardingOperationResponse>> ResendInvitationAsync(Guid tenantId, string idempotencyKey,
        Guid actorId, CancellationToken cancellationToken);
}
