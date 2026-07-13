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
    public Guid SubscriptionId { get; protected set; }
    public string InvoiceType { get; protected set; } = SubscriptionBillingAlignmentConstants.InvoiceTypeSubscription;
    public string CurrencyCode { get; protected set; } = "LKR";
    public decimal SubtotalAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal PaidAmount { get; protected set; }
    public decimal BalanceDue { get; protected set; }
    public DateTimeOffset? BillingPeriodStart { get; protected set; }
    public DateTimeOffset? BillingPeriodEnd { get; protected set; }
    public string? BillingDetailsJson { get; protected set; }
    public DateTimeOffset? IssuedAt { get; protected set; }
    public DateTimeOffset? PaidAt { get; protected set; }
    public DateTimeOffset? VoidedAt { get; protected set; }

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
        return CreateDraft(
            id,
            tenantId,
            tenantSubscriptionId,
            invoiceNumber,
            totalAmount,
            billingCycle,
            dueAt,
            currencyCode: null,
            billingPeriodStart: null,
            billingPeriodEnd: null,
            now);
    }

    public static SubscriptionInvoice CreateDraft(
        Guid id,
        Guid tenantId,
        Guid tenantSubscriptionId,
        string invoiceNumber,
        decimal totalAmount,
        string? billingCycle,
        DateTimeOffset? dueAt,
        string? currencyCode,
        DateTimeOffset? billingPeriodStart,
        DateTimeOffset? billingPeriodEnd,
        DateTimeOffset now)
    {
        var normalizedTotal = Math.Max(0m, totalAmount);
        var normalizedCurrency = string.IsNullOrWhiteSpace(currencyCode)
            ? "LKR"
            : currencyCode.Trim().ToUpperInvariant();

        return new SubscriptionInvoice
        {
            Id = id,
            TenantId = tenantId,
            TenantSubscriptionId = tenantSubscriptionId,
            SubscriptionId = tenantSubscriptionId,
            InvoiceNumber = invoiceNumber,
            TotalAmount = normalizedTotal,
            InvoiceStatus = TenantSubscriptionBillingConstants.InvoiceStatusDraft,
            BillingCycle = billingCycle,
            DueAt = dueAt,
            InvoiceType = SubscriptionBillingAlignmentConstants.InvoiceTypeSubscription,
            CurrencyCode = normalizedCurrency,
            SubtotalAmount = normalizedTotal,
            DiscountAmount = 0m,
            TaxAmount = 0m,
            PaidAmount = 0m,
            BalanceDue = normalizedTotal,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
