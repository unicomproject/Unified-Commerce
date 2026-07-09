using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionCreditNote : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string CreditNoteNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public decimal TotalCreditAmount { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public string? Reason { get; protected set; }
    public string CurrencyCode { get; protected set; } = "LKR";
    public decimal SubtotalAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public DateTimeOffset? IssuedAt { get; protected set; }
    public DateTimeOffset? AppliedAt { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
}
