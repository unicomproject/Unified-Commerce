using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;

public sealed class PaymentWebhookEventDeduplicator : IPaymentWebhookEventDeduplicator
{
    private const string UniqueViolationSqlState = "23505";

    private readonly EPosDbContext _dbContext;

    public PaymentWebhookEventDeduplicator(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryRecordAsync(
        string provider,
        string externalEventId,
        string eventType,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var record = PaymentProviderWebhookEvent.Create(Guid.NewGuid(), provider, externalEventId, eventType, now);
        _dbContext.PaymentProviderWebhookEvents.Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            _dbContext.Entry(record).State = EntityState.Detached;
            return false;
        }
    }
}
