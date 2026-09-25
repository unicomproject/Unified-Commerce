using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;

public sealed class OnlineCheckoutPaymentConfirmationRepository : IOnlineCheckoutPaymentConfirmationRepository
{
    private const decimal AmountMatchEpsilon = 0.01m;

    private readonly EPosDbContext _dbContext;

    public OnlineCheckoutPaymentConfirmationRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OnlineCheckoutPaymentConfirmationResult> ApplyCheckoutCompletedAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        decimal paidAmount,
        string currencyCode,
        string? providerSessionId,
        string? externalReference,
        string? providerResponseJson,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == salesOrderId, cancellationToken);
        var payment = await _dbContext.SalesPayments.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == salesPaymentId, cancellationToken);
        if (order is null || payment is null) return OnlineCheckoutPaymentConfirmationResult.NotFound();

        if (!string.Equals(payment.PaymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            // A completed webhook arriving after the payment already reached PAID is a benign
            // duplicate delivery — safe (and necessary) to re-notify. Arriving after any other
            // terminal state (e.g. FAILED/CANCELLED via a prior expiry) is a real conflict: never
            // silently flip it to paid.
            return string.Equals(payment.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase)
                ? OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber)
                : OnlineCheckoutPaymentConfirmationResult.Anomaly(
                    order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber, "payment_status_conflict");
        }

        var transaction = await _dbContext.SalesPaymentTransactions.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.SalesPaymentId == salesPaymentId && x.TransactionStatus == "PENDING",
            cancellationToken);

        var mismatch = DetectMismatch(payment, transaction, paidAmount, currencyCode, providerSessionId);
        if (mismatch is not null)
            return OnlineCheckoutPaymentConfirmationResult.Anomaly(
                order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber, mismatch);

        order.MarkOnlinePaymentSucceeded(paidAmount, now);
        payment.MarkPaid(paidAmount, externalReference, now);
        transaction?.MarkSucceeded(externalReference, providerResponseJson, now);

        return await SaveGuardedAsync(
            tenantId, salesPaymentId, "PAID",
            () => OnlineCheckoutPaymentConfirmationResult.Success(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber),
            () => OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber),
            () => OnlineCheckoutPaymentConfirmationResult.Anomaly(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber, "concurrent_conflict"),
            cancellationToken);
    }

    public async Task<OnlineCheckoutPaymentConfirmationResult> ApplyCheckoutExpiredAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == salesOrderId, cancellationToken);
        var payment = await _dbContext.SalesPayments.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == salesPaymentId, cancellationToken);
        if (order is null || payment is null) return OnlineCheckoutPaymentConfirmationResult.NotFound();

        if (!string.Equals(payment.PaymentStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(payment.PaymentStatus, "FAILED", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(payment.PaymentStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                ? OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber)
                : OnlineCheckoutPaymentConfirmationResult.Anomaly(
                    order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber, "payment_status_conflict");
        }

        var transaction = await _dbContext.SalesPaymentTransactions.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.SalesPaymentId == salesPaymentId && x.TransactionStatus == "PENDING",
            cancellationToken);

        order.CancelForFailedOnlinePayment(reason, now);
        payment.MarkFailedOrCancelled("CANCELLED", reason, now);
        transaction?.MarkFailed(null, now);

        return await SaveGuardedAsync(
            tenantId, salesPaymentId, "CANCELLED",
            () => OnlineCheckoutPaymentConfirmationResult.Success(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber),
            () => OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber),
            () => OnlineCheckoutPaymentConfirmationResult.Anomaly(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber, "concurrent_conflict"),
            cancellationToken);
    }

    /// <summary>
    /// Confirms the webhook is completing the exact checkout it claims to, not merely a payment
    /// bearing the same tenant/order/payment ids. All three checks compare already-derived,
    /// non-secret identifiers (session id, amount, currency) against what was recorded when the
    /// checkout session was created — never provider secrets.
    /// </summary>
    private static string? DetectMismatch(
        SalesPayment payment,
        SalesPaymentTransaction? transaction,
        decimal paidAmount,
        string currencyCode,
        string? providerSessionId)
    {
        if (!string.IsNullOrWhiteSpace(transaction?.ExternalTransactionReference) &&
            !string.IsNullOrWhiteSpace(providerSessionId) &&
            !string.Equals(transaction.ExternalTransactionReference, providerSessionId, StringComparison.Ordinal))
            return "session_mismatch";

        if (!string.Equals(payment.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            return "currency_mismatch";

        if (payment.RequestedAmount is { } requestedAmount &&
            Math.Abs(requestedAmount - paidAmount) > AmountMatchEpsilon)
            return "amount_mismatch";

        return null;
    }

    /// <summary>
    /// Commits the in-memory transition guarded by <see cref="Domain.Modules.Tenant.Payment.Entities.SalesPayment.RowVersion"/>.
    /// Two concurrent deliveries for the same payment both pass the PENDING check above (neither has
    /// committed yet), but only one SaveChanges can win: EF includes the original RowVersion in the
    /// UPDATE's WHERE clause, so the loser gets <see cref="DbUpdateConcurrencyException"/> instead of
    /// silently overwriting the winner's row. The loser then re-reads the committed state and reports
    /// the outcome accordingly, rather than reporting its own (never-applied) attempt as a success.
    /// </summary>
    private async Task<OnlineCheckoutPaymentConfirmationResult> SaveGuardedAsync(
        Guid tenantId,
        Guid salesPaymentId,
        string expectedTerminalStatus,
        Func<OnlineCheckoutPaymentConfirmationResult> onApplied,
        Func<OnlineCheckoutPaymentConfirmationResult> onAlreadyAtExpectedStatus,
        Func<OnlineCheckoutPaymentConfirmationResult> onConflict,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return onApplied();
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries())
                entry.State = EntityState.Detached;

            var currentStatus = await _dbContext.SalesPayments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == salesPaymentId)
                .Select(x => x.PaymentStatus)
                .FirstOrDefaultAsync(cancellationToken);

            return string.Equals(currentStatus, expectedTerminalStatus, StringComparison.OrdinalIgnoreCase)
                ? onAlreadyAtExpectedStatus()
                : onConflict();
        }
    }
}
