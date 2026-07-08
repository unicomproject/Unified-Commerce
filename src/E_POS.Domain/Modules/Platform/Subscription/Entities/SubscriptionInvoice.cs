using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionInvoice : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string InvoiceNumber { get; protected set; } = string.Empty;
    public Guid TenantSubscriptionId { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public string InvoiceStatus { get; protected set; } = TenantSubscriptionBillingConstants.InvoiceStatusDraft;
    public string? BillingCycle { get; protected set; }
    public DateTimeOffset? DueAt { get; protected set; }

    public static SubscriptionInvoice CreateDraft(
        Guid id,
        Guid tenantId,
        Guid tenantSubscriptionId,
        string invoiceNumber,
        decimal totalAmount,
        string? billingCycle,
        DateTimeOffset? dueAt,
        DateTimeOffset now)
    {
        return new SubscriptionInvoice
        {
            Id = id,
            TenantId = tenantId,
            TenantSubscriptionId = tenantSubscriptionId,
            InvoiceNumber = invoiceNumber,
            TotalAmount = totalAmount,
            InvoiceStatus = TenantSubscriptionBillingConstants.InvoiceStatusDraft,
            BillingCycle = billingCycle,
            DueAt = dueAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

