using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;

public sealed class OnlineCheckoutPaymentConfirmationRepository : IOnlineCheckoutPaymentConfirmationRepository
{
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
            return OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(
                order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber);
        }

        order.MarkOnlinePaymentSucceeded(paidAmount, now);
        payment.MarkPaid(paidAmount, externalReference, now);

        var transaction = await _dbContext.SalesPaymentTransactions.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.SalesPaymentId == salesPaymentId && x.TransactionStatus == "PENDING",
            cancellationToken);
        transaction?.MarkSucceeded(externalReference, providerResponseJson, now);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return OnlineCheckoutPaymentConfirmationResult.Success(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber);
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
            return OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(
                order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber);
        }

        order.CancelForFailedOnlinePayment(reason, now);
        payment.MarkFailedOrCancelled("CANCELLED", reason, now);

        var transaction = await _dbContext.SalesPaymentTransactions.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.SalesPaymentId == salesPaymentId && x.TransactionStatus == "PENDING",
            cancellationToken);
        transaction?.MarkFailed(null, now);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return OnlineCheckoutPaymentConfirmationResult.Success(order.CustomerId ?? Guid.Empty, order.Id, order.OrderNumber);
    }
}
