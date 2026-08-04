using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
}
