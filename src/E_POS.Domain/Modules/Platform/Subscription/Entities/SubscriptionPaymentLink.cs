using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPaymentLink : AuditableEntity
{
    public DateTimeOffset ExpiresAt { get; protected set; }
    public string PaymentLinkTokenHash { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public string? ProviderName { get; protected set; }
    public string? ProviderPaymentLinkId { get; protected set; }
    public string PaymentUrl { get; protected set; } = string.Empty;
    public string LinkStatus { get; protected set; } = string.Empty;
    public string? SentToEmail { get; protected set; }
    public DateTimeOffset? SentAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public DateTimeOffset? LastReminderAt { get; protected set; }
    public int ReminderCount { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
}
