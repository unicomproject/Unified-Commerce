using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformTenantOnboardingRepository : IPlatformTenantOnboardingRepository
{
    private readonly EPosDbContext _db;
    public PlatformTenantOnboardingRepository(EPosDbContext db) => _db = db;

    public async Task AddDraftAsync(PlatformTenantOnboardingDraft draft, CancellationToken cancellationToken)
    {
        _db.PlatformTenantOnboardingDrafts.Add(draft);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PlatformTenantOnboardingDraft?> GetDraftAsync(Guid id, CancellationToken cancellationToken, bool tracking = true)
    {
        var query = tracking ? _db.PlatformTenantOnboardingDrafts : _db.PlatformTenantOnboardingDrafts.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformTenantOnboardingDraft>> ListDraftsAsync(Guid actorId, bool includeAll, CancellationToken cancellationToken)
    {
        var query = _db.PlatformTenantOnboardingDrafts.AsNoTracking()
            .Where(x => x.Status == "in_progress" || x.Status == "finalizing");
        if (!includeAll) query = query.Where(x => x.OwnerPlatformUserId == actorId);
        return await query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id).Take(100).ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException ex) { throw new TenantOnboardingConcurrencyException(ex); }
    }

    public Task<PlatformTenantOnboardingOperation?> GetOperationByDraftAsync(Guid draftId, CancellationToken cancellationToken) =>
        _db.PlatformTenantOnboardingOperations.AsNoTracking().SingleOrDefaultAsync(x => x.DraftId == draftId, cancellationToken);

    public Task<PlatformTenantOnboardingOperation?> GetOperationAsync(Guid operationId, CancellationToken cancellationToken) =>
        _db.PlatformTenantOnboardingOperations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken);

    public Task<PlatformTenantOnboardingOperation?> GetOperationByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        _db.PlatformTenantOnboardingOperations.AsNoTracking().Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task AddCompletionAsync(PlatformTenantOnboardingOperation operation, IReadOnlyList<TenantContact> contacts,
        IReadOnlyList<IntegrationOutboxMessage> outboxMessages, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.PlatformTenantOnboardingOperations.Add(operation);
        if (contacts.Count > 0) _db.TenantContacts.AddRange(contacts);
        if (outboxMessages.Count > 0) _db.IntegrationOutboxMessages.AddRange(outboxMessages);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<bool> RetryOperationAsync(Guid operationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var operation = await _db.PlatformTenantOnboardingOperations
            .FromSqlInterpolated($"SELECT * FROM platform_tenant_onboarding_operations WHERE id = {operationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (operation is null) return false;
        var messages = await _db.IntegrationOutboxMessages.Where(x =>
            (x.AggregateId == operationId || x.CorrelationId == operationId) &&
            (x.Status == "FAILED_RETRYABLE" || x.Status == "FAILED_FINAL")).ToListAsync(cancellationToken);
        if (messages.Count == 0) return false;
        foreach (var message in messages) message.RetryNow(now);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<TenantInvitationResendResult> ResendInvitationAsync(Guid tenantId, string keyHash,
        string requestHash, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var tenant = await _db.Tenants
            .FromSqlInterpolated($"SELECT * FROM tenants WHERE id = {tenantId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (tenant is null) return new(TenantInvitationResendOutcome.NotFound);
        if (tenant.Status != TenantStatusConstants.Active) return new(TenantInvitationResendOutcome.InvalidTransition);
        var operation = await _db.PlatformTenantOnboardingOperations.Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (operation is null) return new(TenantInvitationResendOutcome.NotFound);
        var type = "tenant_admin.invitation_resend_requested";
        var dedupe = $"{type}:{tenantId:D}:{keyHash}";
        var existing = await _db.IntegrationOutboxMessages.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DeduplicationKey == dedupe, ct);
        if (existing is not null)
            return existing.PayloadJson.Contains(requestHash, StringComparison.Ordinal)
                ? new(TenantInvitationResendOutcome.Replay)
                : new(TenantInvitationResendOutcome.IdempotencyConflict);
        if (await _db.IntegrationOutboxMessages.AsNoTracking().AnyAsync(x => x.TenantId == tenantId &&
                x.MessageType == type && x.CreatedAt > now.AddMinutes(-1), ct))
            return new(TenantInvitationResendOutcome.RateLimited);
        operation.MarkInvitationPending(now);
        _db.IntegrationOutboxMessages.Add(IntegrationOutboxMessage.Create(Guid.NewGuid(), type,
            "tenant_onboarding", operation.Id, operation.Version, tenantId, operation.Id, null,
            JsonSerializer.Serialize(new { tenantId, operationId = operation.Id, requestHash, actorId }),
            dedupe, now));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(TenantInvitationResendOutcome.Success);
    }
}
