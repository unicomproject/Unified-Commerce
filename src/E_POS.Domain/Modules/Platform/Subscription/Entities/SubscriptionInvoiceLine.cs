using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionInvoiceLine : AuditableEntity
{
    public string LineNumber { get; protected set; } = string.Empty;
    public decimal LineTotalAmount { get; protected set; }
    public decimal Quantity { get; protected set; }
    public Guid SubscriptionInvoiceId { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public int? LineNumberInt { get; protected set; }
    public string ItemType { get; protected set; } = string.Empty;
    public Guid? ItemReferenceId { get; protected set; }
    public string? ItemCode { get; protected set; }
    public string Description { get; protected set; } = string.Empty;
    public decimal UnitPrice { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal LineTotal { get; protected set; }

    public static SubscriptionInvoiceLine Create(
        Guid id,
        Guid subscriptionInvoiceId,
        string lineNumber,
        int? lineNumberInt,
        string itemType,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal lineTotalAmount,
        DateTimeOffset now)
    {
        var normalizedLineTotal = Math.Max(0m, lineTotalAmount);
        return new SubscriptionInvoiceLine
        {
            Id = id,
            SubscriptionInvoiceId = subscriptionInvoiceId,
            InvoiceId = subscriptionInvoiceId,
            LineNumber = lineNumber,
            LineNumberInt = lineNumberInt,
            ItemType = itemType,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = 0m,
            TaxAmount = 0m,
            LineTotal = normalizedLineTotal,
            LineTotalAmount = normalizedLineTotal,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
