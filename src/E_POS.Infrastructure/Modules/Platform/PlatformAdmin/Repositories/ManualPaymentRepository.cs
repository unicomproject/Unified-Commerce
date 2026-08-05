using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class ManualPaymentRepository : IManualPaymentRepository
{
    private readonly EPosDbContext _db;

    public ManualPaymentRepository(EPosDbContext db) => _db = db;

    public async Task<ManualPaymentAccessContext?> FindAccessAsync(string tokenHash, string action, DateTimeOffset now, CancellationToken ct)
    {
        var access = await _db.SubscriptionPaymentLinks.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        if (access is null || !access.Allows(action, now) || access.PaymentTransactionId is null) return null;
        var payment = await _db.SubscriptionPaymentTransactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == access.PaymentTransactionId.Value && x.TenantId == access.TenantId, ct);
        if (payment is null) return null;
        var invoice = await _db.SubscriptionInvoices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == payment.InvoiceId && x.TenantId == access.TenantId, ct);
        var tenant = await _db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == access.TenantId, ct);
        if (invoice is null || tenant is null) return null;
        var subscription = await _db.TenantSubscriptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == invoice.SubscriptionId && x.TenantId == access.TenantId, ct);
        if (subscription is null) return null;
        var planName = await _db.SubscriptionPlans.AsNoTracking().Where(x => x.Id == subscription.SubscriptionPlanId)
            .Select(x => x.Name).SingleOrDefaultAsync(ct) ?? string.Empty;
        var operation = await _db.PlatformTenantOnboardingOperations.AsNoTracking()
            .Where(x => x.TenantId == access.TenantId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        return new(access, payment, invoice, tenant, subscription, planName, operation);
    }

    public async Task RecordAccessAsync(Guid accessId, DateTimeOffset now, CancellationToken ct)
    {
        var access = await _db.SubscriptionPaymentLinks.SingleOrDefaultAsync(x => x.Id == accessId, ct);
        if (access is null) return;
        access.RecordAccess(now);
        if (access.PaymentTransactionId is { } paymentId)
        {
            var payment = await _db.SubscriptionPaymentTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId, ct);
            if (payment is not null)
            {
                var nonce = Hash($"access:{accessId:D}:{now.UtcTicks}:{Guid.NewGuid():N}");
                _db.SubscriptionPaymentReviews.Add(SubscriptionPaymentReview.Create(Guid.NewGuid(), payment.TenantId,
                    payment.Id, payment.InvoiceId, "ACCESS_USED", payment.TransactionStatus, payment.TransactionStatus,
                    "PAYMENT_RECIPIENT", null, null, null, nonce, Hash(accessId.ToString("D")),
                    Guid.NewGuid(), payment.Version, now));
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ManualPaymentStatusResponse?> GetStatusAsync(Guid paymentId, string accessToken, CancellationToken ct)
    {
        var row = await (from payment in _db.SubscriptionPaymentTransactions.AsNoTracking()
                         join invoice in _db.SubscriptionInvoices.AsNoTracking() on payment.InvoiceId equals invoice.Id
                         join tenant in _db.Tenants.AsNoTracking() on payment.TenantId equals tenant.Id
                         join subscription in _db.TenantSubscriptions.AsNoTracking() on invoice.SubscriptionId equals subscription.Id
                         join plan in _db.SubscriptionPlans.AsNoTracking() on subscription.SubscriptionPlanId equals plan.Id
                         where payment.Id == paymentId
                         select new { payment, invoice, tenant, subscription, plan }).SingleOrDefaultAsync(ct);
        if (row is null) return null;
        var invitation = await _db.PlatformTenantOnboardingOperations.AsNoTracking()
            .Where(x => x.TenantId == row.tenant.Id).OrderByDescending(x => x.CreatedAt)
            .Select(x => x.InvitationStatus).FirstOrDefaultAsync(ct) ?? "NOT_ELIGIBLE";
        var evidence = await EvidenceDtos(paymentId).ToListAsync(ct);
        var escaped = Uri.EscapeDataString(accessToken);
        var statusUrl = $"/api/v1/tenant-onboarding/payment-access/{escaped}";
        return new(row.tenant.Id, row.tenant.TenantCode, row.payment.Id, row.invoice.Id, row.invoice.InvoiceNumber,
            row.payment.ExpectedAmount, row.invoice.TaxAmount, row.invoice.TotalAmount, row.payment.CurrencyCode,
            row.invoice.DueAt, row.payment.TransactionStatus, row.payment.Version, row.plan.Name,
            row.subscription.BillingCycle, "Pay by the configured manual method and submit a clear PDF, JPEG, or PNG proof.",
            $"{statusUrl}/invoice", statusUrl, null, row.tenant.Status, invitation,
            row.tenant.DisplayName, row.subscription.SubscriptionStatus, row.subscription.CurrentPeriodStart,
            row.subscription.CurrentPeriodEnd, row.invoice.InvoiceStatus, row.invoice.SubtotalAmount,
            row.payment.PaymentMethod, Suffix(row.payment.ManualReference), row.payment.SubmittedAmount,
            row.payment.PaymentDate, row.payment.PayerNote, evidence, row.payment.SubmittedAt,
            row.payment.PaidAt, row.payment.UpdatedAt ?? row.payment.CreatedAt);
    }

    public async Task<ManualPaymentSubmitResult> SubmitAsync(ManualPaymentSubmitCommand command, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var payment = await _db.SubscriptionPaymentTransactions
            .FromSqlInterpolated($"SELECT * FROM subscription_payment_transactions WHERE id = {command.PaymentId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        var access = await _db.SubscriptionPaymentLinks
            .FromSqlInterpolated($"SELECT * FROM subscription_payment_links WHERE id = {command.AccessId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (payment is null) return new(ManualPaymentMutationOutcome.NotFound);
        if (access is null || access.PaymentTransactionId != payment.Id || !access.Allows("EVIDENCE", command.Now))
            return new(ManualPaymentMutationOutcome.InvalidAccess);

        if (payment.LastCommandIdempotencyKeyHash == command.IdempotencyKeyHash)
        {
            if (payment.LastCommandRequestHash != command.RequestHash)
                return new(ManualPaymentMutationOutcome.IdempotencyConflict);
            await tx.CommitAsync(ct);
            return new(ManualPaymentMutationOutcome.Replay, await BuildSubmissionAsync(payment.Id, true, ct));
        }
        if (command.ExpectedVersion.HasValue && payment.Version != command.ExpectedVersion.Value)
            return new(ManualPaymentMutationOutcome.ConcurrencyConflict);
        if (!ManualPaymentConstants.CanSubmit(payment.TransactionStatus))
            return new(ManualPaymentMutationOutcome.InvalidTransition);
        if (!string.Equals(payment.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            return new(ManualPaymentMutationOutcome.CurrencyMismatch);
        if (payment.ExpectedAmount != command.SubmittedAmount)
            return new(ManualPaymentMutationOutcome.AmountMismatch);

        var before = payment.SubmitManual(command.SubmittedAmount, command.CurrencyCode, command.PaymentMethod,
            command.Reference, command.PaymentDate, command.PayerNote, command.IdempotencyKeyHash,
            command.RequestHash, "PAYMENT_RECIPIENT", null, command.Now);

        var oldEvidence = await _db.SubscriptionPaymentEvidence
            .Where(x => x.PaymentId == payment.Id && x.IsActive).ToListAsync(ct);
        foreach (var old in oldEvidence) old.Supersede(command.Now);
        var evidence = SubscriptionPaymentEvidence.Create(command.Evidence.Id, payment.TenantId, payment.Id,
            payment.InvoiceId, command.Evidence.Container, command.Evidence.StorageKey,
            command.Evidence.OriginalFileName, command.Evidence.SafeFileName, command.Evidence.ContentType,
            command.Evidence.Length, command.Evidence.Sha256, payment.SubmissionVersion,
            command.Evidence.ScanStatus, command.Now);
        _db.SubscriptionPaymentEvidence.Add(evidence);
        _db.SubscriptionPaymentReviews.Add(SubscriptionPaymentReview.Create(Guid.NewGuid(), payment.TenantId,
            payment.Id, payment.InvoiceId, before == ManualPaymentConstants.AwaitingPayment ? "SUBMIT" : "RESUBMIT",
            before, payment.TransactionStatus, "PAYMENT_RECIPIENT", null, null, null,
            command.IdempotencyKeyHash, command.RequestHash, command.CorrelationId, payment.Version, command.Now,
            payment.SubmittedAmount, payment.ExpectedAmount, payment.CurrencyCode, evidence.Id, evidence.SubmissionVersion));
        var operation = await _db.PlatformTenantOnboardingOperations
            .Where(x => x.TenantId == payment.TenantId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        operation?.MarkPaymentSubmitted(command.Now);
        AddOutbox("manual_payment.submitted_notification_requested", payment, command.CorrelationId,
            command.IdempotencyKeyHash, command.Now);
        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            return new(ManualPaymentMutationOutcome.ConcurrencyConflict);
        }
        return new(ManualPaymentMutationOutcome.Success, await BuildSubmissionAsync(payment.Id, false, ct));
    }

    public async Task<ManualPaymentReviewHistoryResponse?> GetHistoryAsync(Guid paymentId, bool includeActor, CancellationToken ct)
    {
        if (!await _db.SubscriptionPaymentTransactions.AsNoTracking().AnyAsync(x => x.Id == paymentId, ct)) return null;
        var items = await _db.SubscriptionPaymentReviews.AsNoTracking().Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => new ManualPaymentReviewHistoryItem(x.Id, x.Action, x.StatusBefore, x.StatusAfter,
                x.ReasonCode, x.ReviewNote, x.ActorType, includeActor ? x.ActorId : null,
                x.PaymentVersion, x.CreatedAt)).ToListAsync(ct);
        return new(paymentId, items);
    }

    public async Task<ManualPaymentQueueResponse> GetQueueAsync(ManualPaymentQueueQuery query, CancellationToken ct)
    {
        var q =
            from payment in _db.SubscriptionPaymentTransactions.AsNoTracking()
            join invoice in _db.SubscriptionInvoices.AsNoTracking() on payment.InvoiceId equals invoice.Id
            join tenant in _db.Tenants.AsNoTracking() on payment.TenantId equals tenant.Id
            join subscription in _db.TenantSubscriptions.AsNoTracking() on invoice.SubscriptionId equals subscription.Id
            join plan in _db.SubscriptionPlans.AsNoTracking() on subscription.SubscriptionPlanId equals plan.Id
            where payment.ProviderName == ManualPaymentConstants.Provider
            select new { Payment = payment, Invoice = invoice, Tenant = tenant, Subscription = subscription, Plan = plan };
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToUpperInvariant();
            q = q.Where(x => x.Payment.TransactionStatus == status);
        }
        if (query.TenantId.HasValue) q = q.Where(x => x.Tenant.Id == query.TenantId.Value);
        if (query.PlanId.HasValue) q = q.Where(x => x.Plan.Id == query.PlanId.Value);
        if (query.SubmittedFrom.HasValue) q = q.Where(x => x.Payment.SubmittedAt >= query.SubmittedFrom.Value);
        if (query.SubmittedTo.HasValue) q = q.Where(x => x.Payment.SubmittedAt <= query.SubmittedTo.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Tenant.TenantCode, pattern) ||
                             EF.Functions.ILike(x.Tenant.DisplayName, pattern) ||
                             EF.Functions.ILike(x.Invoice.InvoiceNumber, pattern));
        }
        var page = Math.Max(1, query.PageNumber);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var total = await q.CountAsync(ct);
        var ascending = query.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        var ordered = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "amount" => ascending ? q.OrderBy(x => x.Payment.ExpectedAmount) : q.OrderByDescending(x => x.Payment.ExpectedAmount),
            "status" => ascending ? q.OrderBy(x => x.Payment.TransactionStatus) : q.OrderByDescending(x => x.Payment.TransactionStatus),
            "tenant" => ascending ? q.OrderBy(x => x.Tenant.DisplayName) : q.OrderByDescending(x => x.Tenant.DisplayName),
            _ => ascending ? q.OrderBy(x => x.Payment.SubmittedAt) : q.OrderByDescending(x => x.Payment.SubmittedAt)
        };
        var rows = await ordered.ThenByDescending(x => x.Payment.UpdatedAt ?? x.Payment.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new ManualPaymentQueueItem(x.Payment.Id, x.Tenant.Id, x.Tenant.TenantCode, x.Tenant.DisplayName,
                x.Tenant.Status, x.Invoice.Id, x.Invoice.InvoiceNumber, x.Subscription.Id, x.Plan.Id, x.Plan.Name,
                x.Subscription.BillingCycle, x.Invoice.DueAt, x.Payment.ExpectedAmount, x.Payment.SubmittedAmount,
                x.Payment.CurrencyCode, x.Payment.TransactionStatus, x.Payment.Version, x.Payment.SubmittedAt, null,
                x.Payment.UpdatedAt ?? x.Payment.CreatedAt))
            .ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return new(rows.Select(x => x with { SubmittedAgeSeconds = x.SubmittedAt.HasValue
            ? Math.Max(0, (long)(now - x.SubmittedAt.Value).TotalSeconds) : null }).ToList(),
            page, size, total, (int)Math.Ceiling(total / (double)size));
    }

    public async Task<ManualPaymentDetailResponse?> GetDetailAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await (
            from transaction in _db.SubscriptionPaymentTransactions.AsNoTracking()
            join invoice in _db.SubscriptionInvoices.AsNoTracking() on transaction.InvoiceId equals invoice.Id
            join tenant in _db.Tenants.AsNoTracking() on transaction.TenantId equals tenant.Id
            join subscription in _db.TenantSubscriptions.AsNoTracking() on invoice.SubscriptionId equals subscription.Id
            join plan in _db.SubscriptionPlans.AsNoTracking() on subscription.SubscriptionPlanId equals plan.Id
            where transaction.ProviderName == ManualPaymentConstants.Provider && transaction.Id == paymentId
            select new ManualPaymentQueueItem(transaction.Id, tenant.Id, tenant.TenantCode, tenant.DisplayName, tenant.Status,
                invoice.Id, invoice.InvoiceNumber, subscription.Id, plan.Id, plan.Name, subscription.BillingCycle, invoice.DueAt,
                transaction.ExpectedAmount, transaction.SubmittedAmount, transaction.CurrencyCode, transaction.TransactionStatus,
                transaction.Version, transaction.SubmittedAt, null, transaction.UpdatedAt ?? transaction.CreatedAt))
            .SingleOrDefaultAsync(ct);
        if (payment is null) return null;
        var details = await (from transaction in _db.SubscriptionPaymentTransactions.AsNoTracking()
                             join invoice in _db.SubscriptionInvoices.AsNoTracking() on transaction.InvoiceId equals invoice.Id
                             join subscription in _db.TenantSubscriptions.AsNoTracking() on invoice.SubscriptionId equals subscription.Id
                             where transaction.Id == paymentId
                             select new
                             {
                                 transaction.PaymentMethod,
                                 transaction.ManualReference,
                                 transaction.PaymentDate,
                                 transaction.PayerNote,
                                 transaction.SubmittedByType,
                                 subscription.SubscriptionStatus,
                                 invoice.InvoiceStatus,
                                 invoice.SubtotalAmount,
                                 invoice.TaxAmount
                             }).SingleAsync(ct);
        var evidence = await EvidenceDtos(paymentId).ToListAsync(ct);
        var history = (await GetHistoryAsync(paymentId, true, ct))!.Items;
        var invitation = await _db.PlatformTenantOnboardingOperations.AsNoTracking()
            .Where(x => x.TenantId == payment.TenantId).OrderByDescending(x => x.CreatedAt)
            .Select(x => x.InvitationStatus).FirstOrDefaultAsync(ct) ?? "NOT_ELIGIBLE";
        return new(payment, details.PaymentMethod, Suffix(details.ManualReference), details.PaymentDate,
            details.PayerNote, evidence, history, AllowedActions(payment.Status),
            payment.Status == ManualPaymentConstants.Paid && payment.TenantStatus == TenantStatusConstants.PendingActivation,
            details.SubscriptionStatus, details.InvoiceStatus, details.SubtotalAmount, details.TaxAmount,
            invitation, details.SubmittedByType);
    }

    public Task<SubscriptionPaymentEvidence?> GetEvidenceAsync(Guid paymentId, Guid evidenceId, CancellationToken ct) =>
        _db.SubscriptionPaymentEvidence.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == evidenceId && x.PaymentId == paymentId && x.IsActive, ct);

    public async Task RecordProofAccessAsync(Guid paymentId, Guid evidenceId, Guid actorId, Guid correlationId,
        DateTimeOffset now, CancellationToken ct)
    {
        var payment = await _db.SubscriptionPaymentTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId, ct);
        if (payment is null) return;
        var nonce = Hash($"proof:{paymentId:D}:{evidenceId:D}:{actorId:D}:{now.UtcTicks}:{Guid.NewGuid():N}");
        _db.SubscriptionPaymentReviews.Add(SubscriptionPaymentReview.Create(Guid.NewGuid(), payment.TenantId,
            payment.Id, payment.InvoiceId, "PROOF_VIEWED", payment.TransactionStatus, payment.TransactionStatus,
            "PLATFORM_ADMIN", actorId, null, null, nonce, Hash(evidenceId.ToString("D")), correlationId,
            payment.Version, now));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ManualPaymentReviewResult> ReviewAsync(ManualPaymentReviewCommand command, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var previous = await _db.SubscriptionPaymentReviews.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentId == command.PaymentId && x.IdempotencyKeyHash == command.IdempotencyKeyHash, ct);
        if (previous is not null)
        {
            if (previous.RequestHash != command.RequestHash) return new(ManualPaymentMutationOutcome.IdempotencyConflict);
            var replay = await BuildReviewResponseAsync(command.PaymentId, previous.Id, previous.Action, true, ct);
            await tx.CommitAsync(ct);
            return new(ManualPaymentMutationOutcome.Replay, replay);
        }

        var payment = await _db.SubscriptionPaymentTransactions
            .FromSqlInterpolated($"SELECT * FROM subscription_payment_transactions WHERE id = {command.PaymentId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (payment is null) return new(ManualPaymentMutationOutcome.NotFound);
        if (payment.Version != command.ExpectedVersion) return new(ManualPaymentMutationOutcome.ConcurrencyConflict);
        if (!ManualPaymentConstants.CanReview(payment.TransactionStatus)) return new(ManualPaymentMutationOutcome.InvalidTransition);

        var action = command.Action.Trim().ToUpperInvariant();
        SubscriptionPaymentEvidence? evidenceSnapshot = null;
        if (action == ManualPaymentConstants.Approve)
        {
            if (payment.SubmittedAmount != payment.ExpectedAmount) return new(ManualPaymentMutationOutcome.AmountMismatch);
            var evidence = await _db.SubscriptionPaymentEvidence.AsNoTracking()
                .Where(x => x.PaymentId == payment.Id && x.IsActive).ToListAsync(ct);
            if (evidence.Count == 0) return new(ManualPaymentMutationOutcome.ProofRequired);
            if (evidence.Any(x => x.ScanStatus != ManualPaymentConstants.ScanClean))
                return new(ManualPaymentMutationOutcome.ProofNotClean);
            evidenceSnapshot = evidence.OrderByDescending(x => x.SubmissionVersion).First();
        }
        else if (action is not (ManualPaymentConstants.Reject or ManualPaymentConstants.RequestInformation))
            return new(ManualPaymentMutationOutcome.InvalidTransition);

        evidenceSnapshot ??= await _db.SubscriptionPaymentEvidence.AsNoTracking()
            .Where(x => x.PaymentId == payment.Id && x.IsActive)
            .OrderByDescending(x => x.SubmissionVersion).FirstOrDefaultAsync(ct);

        var invoice = await _db.SubscriptionInvoices.SingleAsync(x => x.Id == payment.InvoiceId, ct);
        var tenant = await _db.Tenants.SingleAsync(x => x.Id == payment.TenantId, ct);
        if (invoice.TenantId != payment.TenantId ||
            invoice.TenantSubscriptionId != payment.TenantSubscriptionId ||
            invoice.TotalAmount != payment.ExpectedAmount ||
            !string.Equals(invoice.CurrencyCode, payment.CurrencyCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(tenant.Status, TenantStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase))
            return new(ManualPaymentMutationOutcome.InvalidTransition);

        var before = payment.BeginReview(command.Now);
        if (action == ManualPaymentConstants.Approve)
            payment.Approve(command.ReviewerId, payment.SubmittedAmount!.Value, command.ReviewNote, command.Now);
        else if (action == ManualPaymentConstants.Reject)
            payment.Reject(command.ReviewerId, command.ReasonCode!, command.ReviewNote!, command.Now);
        else
            payment.RequestInformation(command.ReviewerId, command.ReasonCode!, command.ReviewNote!, command.Now);

        if (action == ManualPaymentConstants.Approve)
        {
            if (invoice.InvoiceStatus == TenantSubscriptionBillingConstants.InvoiceStatusDraft) invoice.Issue(command.Now);
            if (invoice.InvoiceStatus == TenantSubscriptionBillingConstants.InvoiceStatusPending) invoice.MarkPaid(command.Now, command.Now);
            if (tenant.Status == TenantStatusConstants.PendingPayment) tenant.MarkPendingActivation(command.ReviewerId, command.Now);
        }

        var reviewId = Guid.NewGuid();
        var review = SubscriptionPaymentReview.Create(reviewId, payment.TenantId, payment.Id, payment.InvoiceId,
            action, before, payment.TransactionStatus, "PLATFORM_ADMIN", command.ReviewerId,
            command.ReviewNote, command.ReasonCode, command.IdempotencyKeyHash, command.RequestHash,
            command.CorrelationId, payment.Version, command.Now, payment.SubmittedAmount,
            payment.ExpectedAmount, payment.CurrencyCode, evidenceSnapshot?.Id,
            evidenceSnapshot?.SubmissionVersion);
        _db.SubscriptionPaymentReviews.Add(review);
        var operation = await _db.PlatformTenantOnboardingOperations
            .Where(x => x.TenantId == payment.TenantId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        operation?.MarkPaymentReviewOutcome(payment.TransactionStatus, command.Now);
        AddOutbox($"manual_payment.{OutcomeEvent(action)}_notification_requested", payment,
            command.CorrelationId, command.IdempotencyKeyHash, command.Now);
        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            return new(ManualPaymentMutationOutcome.ConcurrencyConflict);
        }
        return new(ManualPaymentMutationOutcome.Success,
            await BuildReviewResponseAsync(payment.Id, reviewId, action, false, ct));
    }

    public async Task<ManualPaymentNotificationResult> ResendNotificationAsync(Guid paymentId, string notificationType,
        string? reason, string keyHash, string requestHash, Guid correlationId, Guid actorId,
        DateTimeOffset now, CancellationToken ct)
    {
        var payment = await _db.SubscriptionPaymentTransactions.SingleOrDefaultAsync(x => x.Id == paymentId, ct);
        if (payment is null) return new(ManualPaymentMutationOutcome.NotFound);
        var type = notificationType.Trim().ToUpperInvariant();
        var dedupe = $"manual-payment:resend:{paymentId:D}:{keyHash}";
        var existing = await _db.IntegrationOutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.DeduplicationKey == dedupe, ct);
        if (existing is not null)
            return existing.PayloadJson.Contains(requestHash, StringComparison.Ordinal)
                ? new(ManualPaymentMutationOutcome.Replay, new(paymentId, type, existing.Status, true))
                : new(ManualPaymentMutationOutcome.IdempotencyConflict);
        if (await _db.IntegrationOutboxMessages.AsNoTracking().AnyAsync(x => x.AggregateId == paymentId &&
                x.MessageType == "manual_payment.notification_resend_requested" && x.CreatedAt > now.AddMinutes(-1), ct))
            return new(ManualPaymentMutationOutcome.RateLimited);
        _db.IntegrationOutboxMessages.Add(IntegrationOutboxMessage.Create(Guid.NewGuid(),
            "manual_payment.notification_resend_requested", "manual_payment", payment.Id, payment.Version,
            payment.TenantId, correlationId, null,
            JsonSerializer.Serialize(new { paymentId, notificationType = type, reason, requestHash, actorId }),
            dedupe, now));
        await _db.SaveChangesAsync(ct);
        return new(ManualPaymentMutationOutcome.Success, new(paymentId, type, "PENDING", false));
    }

    private IQueryable<ManualPaymentEvidenceDto> EvidenceDtos(Guid paymentId) =>
        _db.SubscriptionPaymentEvidence.AsNoTracking().Where(x => x.PaymentId == paymentId && x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ManualPaymentEvidenceDto(x.Id, x.SafeFileName, x.ContentType, x.FileSize,
                x.ScanStatus, x.SubmissionVersion, x.CreatedAt));

    private async Task<ManualPaymentSubmissionResponse> BuildSubmissionAsync(Guid paymentId, bool replay, CancellationToken ct)
    {
        var p = await _db.SubscriptionPaymentTransactions.AsNoTracking().SingleAsync(x => x.Id == paymentId, ct);
        var evidence = await EvidenceDtos(paymentId).ToListAsync(ct);
        return new(p.Id, p.TransactionStatus, p.Version, Suffix(p.ManualReference), p.ExpectedAmount,
            p.SubmittedAmount, p.CurrencyCode, p.PaymentDate, evidence, p.SubmittedAt,
            p.UpdatedAt ?? p.CreatedAt, p.TransactionStatus == ManualPaymentConstants.PaymentSubmitted
                ? "WAIT_FOR_REVIEW" : "FOLLOW_REVIEW_INSTRUCTIONS", replay);
    }

    private async Task<ManualPaymentReviewResponse> BuildReviewResponseAsync(Guid paymentId, Guid reviewId,
        string action, bool replay, CancellationToken ct)
    {
        var row = await (from payment in _db.SubscriptionPaymentTransactions.AsNoTracking()
                         join invoice in _db.SubscriptionInvoices.AsNoTracking() on payment.InvoiceId equals invoice.Id
                         join tenant in _db.Tenants.AsNoTracking() on payment.TenantId equals tenant.Id
                         where payment.Id == paymentId
                         select new { payment, invoice, tenant }).SingleAsync(ct);
        return new(paymentId, row.invoice.Id, row.tenant.Id, row.payment.TransactionStatus,
            row.invoice.InvoiceStatus, row.tenant.Status, row.payment.Version, reviewId, action,
            row.payment.TransactionStatus == ManualPaymentConstants.Paid, replay);
    }

    private void AddOutbox(string type, SubscriptionPaymentTransaction payment, Guid correlationId,
        string keyHash, DateTimeOffset now) => _db.IntegrationOutboxMessages.Add(
        IntegrationOutboxMessage.Create(Guid.NewGuid(), type, "manual_payment", payment.Id,
            payment.Version, payment.TenantId, correlationId, null,
            JsonSerializer.Serialize(new { paymentId = payment.Id, invoiceId = payment.InvoiceId, tenantId = payment.TenantId }),
            $"{type}:{payment.Id:D}:{keyHash}", now));

    private static string OutcomeEvent(string action) => action switch
    {
        ManualPaymentConstants.Approve => "approved",
        ManualPaymentConstants.Reject => "rejected",
        _ => "action_required"
    };

    private static string? Suffix(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : value.Length <= 4 ? value : $"***{value[^4..]}";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static IReadOnlyList<string> AllowedActions(string status) => status switch
    {
        ManualPaymentConstants.PaymentSubmitted or ManualPaymentConstants.UnderReview =>
            [ManualPaymentConstants.Approve, ManualPaymentConstants.Reject, ManualPaymentConstants.RequestInformation],
        _ => []
    };
}
