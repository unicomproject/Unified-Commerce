using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed partial class PlatformTenantRepository
{
    public async Task<PlatformTenantActivationRuntimeResult> ActivateTenantRuntimeAsync(Guid tenantId,
        Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        var tenant = await _dbContext.Tenants
            .FromSqlInterpolated($"SELECT * FROM tenants WHERE id = {tenantId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (tenant is null) return new(PlatformTenantActivationRuntimeOutcome.NotFound);
        if (string.Equals(tenant.Status, TenantStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
        {
            await tx.CommitAsync(ct);
            return new(PlatformTenantActivationRuntimeOutcome.Replay);
        }
        if (!string.Equals(tenant.Status, TenantStatusConstants.PendingActivation, StringComparison.OrdinalIgnoreCase))
            return new(PlatformTenantActivationRuntimeOutcome.InvalidTransition);

        var subscription = await _dbContext.TenantSubscriptions
            .FromSqlInterpolated($"SELECT * FROM tenant_subscriptions WHERE tenant_id = {tenantId} ORDER BY created_at DESC LIMIT 1 FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (subscription is null) return new(PlatformTenantActivationRuntimeOutcome.SubscriptionMissing);
        var verifiedPayment = await (from payment in _dbContext.SubscriptionPaymentTransactions.AsNoTracking()
                                     join invoice in _dbContext.SubscriptionInvoices.AsNoTracking()
                                         on payment.InvoiceId equals invoice.Id
                                     where payment.TenantId == tenantId && invoice.TenantId == tenantId &&
                                           payment.TenantSubscriptionId == subscription.Id &&
                                           invoice.TenantSubscriptionId == subscription.Id &&
                                           payment.TransactionStatus == ManualPaymentConstants.Paid &&
                                           invoice.InvoiceStatus == TenantSubscriptionBillingConstants.InvoiceStatusPaid &&
                                           payment.ExpectedAmount == invoice.TotalAmount &&
                                           payment.ApprovedAmount == invoice.TotalAmount &&
                                           payment.CurrencyCode == invoice.CurrencyCode
                                     select payment.Id).AnyAsync(ct);
        if (!verifiedPayment) return new(PlatformTenantActivationRuntimeOutcome.PaymentNotVerified);

        var adminUserId = await _dbContext.TenantUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && (x.AccountStatus == "INVITED" || x.AccountStatus == "ACTIVE"))
            .OrderBy(x => x.CreatedAt).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!adminUserId.HasValue || !await _dbContext.TenantUserRoles.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.TenantUserId == adminUserId.Value, ct))
            return new(PlatformTenantActivationRuntimeOutcome.MembershipMissing);

        var requiredFeatures = await _dbContext.SubscriptionPlanFeatures.AsNoTracking()
            .Where(x => x.SubscriptionPlanId == subscription.SubscriptionPlanId &&
                        x.Status == SubscriptionPlanConstants.PlanFeatureStatus.Included)
            .Select(x => x.PlatformFeatureId).ToListAsync(ct);
        if (requiredFeatures.Count > 0)
        {
            var readyFeatures = await _dbContext.TenantFeatureEntitlements.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsEnabled && requiredFeatures.Contains(x.PlatformFeatureId))
                .Select(x => x.PlatformFeatureId).Distinct().CountAsync(ct);
            if (readyFeatures != requiredFeatures.Distinct().Count())
                return new(PlatformTenantActivationRuntimeOutcome.EntitlementsNotReady);
        }

        var oldTenantStatus = tenant.Status;
        var oldSubscriptionStatus = subscription.SubscriptionStatus;
        tenant.Activate(actorId, now);
        subscription.Activate(now);
        var sequence = (await _dbContext.TenantSubscriptionHistory.Where(x => x.TenantId == tenantId)
            .MaxAsync(x => (int?)x.SequenceNumber, ct) ?? 0) + 1;
        _dbContext.TenantSubscriptionHistory.Add(TenantSubscriptionHistory.CreateEvent(Guid.NewGuid(),
            tenantId, subscription.Id, sequence, "tenant.activated", now,
            oldStatus: oldSubscriptionStatus, newStatus: subscription.SubscriptionStatus,
            reason: "Tenant activated by platform admin.",
            changeData: JsonSerializer.Serialize(new { oldTenantStatus, newTenantStatus = tenant.Status }),
            changedByPlatformUserId: actorId));

        var operation = await _dbContext.PlatformTenantOnboardingOperations
            .Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (operation is not null)
        {
            operation.MarkActivationCompleted(now);
            var type = "tenant_admin.invitation_requested";
            var dedupe = $"{type}:{tenantId:D}";
            if (!await _dbContext.IntegrationOutboxMessages.AnyAsync(x => x.DeduplicationKey == dedupe, ct))
                _dbContext.IntegrationOutboxMessages.Add(IntegrationOutboxMessage.Create(Guid.NewGuid(), type,
                    "tenant_onboarding", operation.Id, operation.Version, tenantId, operation.Id, null,
                    JsonSerializer.Serialize(new { tenantId, operationId = operation.Id }), dedupe, now));
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(PlatformTenantActivationRuntimeOutcome.Success);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            return new(PlatformTenantActivationRuntimeOutcome.ConcurrencyConflict);
        }
    }
}
