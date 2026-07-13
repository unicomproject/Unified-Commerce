using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

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

    public static SubscriptionPaymentLink CreatePending(
        Guid id,
        Guid tenantId,
        Guid subscriptionInvoiceId,
        string tokenHash,
        string paymentUrl,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        Guid? createdByPlatformUserId = null,
        string? providerName = null,
        string? providerPaymentLinkId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentUrl);

        var normalizedTokenHash = tokenHash.Trim();
        return new SubscriptionPaymentLink
        {
            Id = id,
            TenantId = tenantId,
            SubscriptionInvoiceId = subscriptionInvoiceId,
            InvoiceId = subscriptionInvoiceId,
            PaymentLinkTokenHash = normalizedTokenHash,
            TokenHash = normalizedTokenHash,
            PaymentUrl = paymentUrl.Trim(),
            ExpiresAt = expiresAt,
            LinkStatus = SubscriptionBillingAlignmentConstants.PaymentLinkStatusActive,
            ProviderName = NormalizeOptional(providerName),
            ProviderPaymentLinkId = NormalizeOptional(providerPaymentLinkId),
            CreatedByPlatformUserId = createdByPlatformUserId,
            ReminderCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSent(string sentToEmail, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sentToEmail);

        SentToEmail = sentToEmail.Trim();
        SentAt = now;
        UpdatedAt = now;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        UsedAt = now;
        LinkStatus = SubscriptionBillingAlignmentConstants.PaymentLinkStatusUsed;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
        LinkStatus = SubscriptionBillingAlignmentConstants.PaymentLinkStatusRevoked;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
