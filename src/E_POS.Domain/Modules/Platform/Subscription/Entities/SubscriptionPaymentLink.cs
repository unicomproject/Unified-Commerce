using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPaymentLink : AuditableEntity
{
    public DateTimeOffset ExpiresAt { get; protected set; }
    public string? PaymentLinkTokenHash { get; protected set; }
    public Guid SubscriptionInvoiceId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public string? TokenHash { get; protected set; }
    public string? ProviderName { get; protected set; }
    public string? ProviderPaymentLinkId { get; protected set; }
    public string? PaymentUrl { get; protected set; }
    public string LinkStatus { get; protected set; } = string.Empty;
    public string? SentToEmail { get; protected set; }
    public DateTimeOffset? SentAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public DateTimeOffset? LastReminderAt { get; protected set; }
    public int ReminderCount { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? PaymentTransactionId { get; protected set; }
    public string Purpose { get; protected set; } = ManualPaymentConstants.AccessPurpose;
    public string AllowedActions { get; protected set; } = "STATUS,INVOICE,EVIDENCE,HISTORY";
    public string RecipientType { get; protected set; } = "BILLING_CONTACT";
    public string? RecipientIdentifierHash { get; protected set; }
    public DateTimeOffset? TokenProvisionedAt { get; protected set; }
    public DateTimeOffset? LastAccessedAt { get; protected set; }
    public long Version { get; protected set; } = 1;

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

    public static SubscriptionPaymentLink CreateManualAccess(Guid id, Guid tenantId, Guid invoiceId,
        Guid paymentTransactionId, string recipientIdentifierHash, DateTimeOffset expiresAt,
        DateTimeOffset now, Guid? createdByPlatformUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientIdentifierHash);
        if (expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt));
        return new SubscriptionPaymentLink
        {
            Id = id,
            TenantId = tenantId,
            SubscriptionInvoiceId = invoiceId,
            InvoiceId = invoiceId,
            PaymentTransactionId = paymentTransactionId,
            RecipientIdentifierHash = recipientIdentifierHash.Trim(),
            Purpose = ManualPaymentConstants.AccessPurpose,
            LinkStatus = ManualPaymentConstants.AccessPendingDelivery,
            ExpiresAt = expiresAt,
            CreatedByPlatformUserId = createdByPlatformUserId,
            ProviderName = ManualPaymentConstants.Provider,
            ReminderCount = 0,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void ProvisionToken(string tokenHash, string sentToEmail, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(sentToEmail);
        if (RevokedAt is not null) throw new InvalidOperationException("Revoked access cannot be provisioned.");
        if (TokenProvisionedAt is not null)
        {
            ReminderCount++;
            LastReminderAt = now;
        }
        PaymentLinkTokenHash = tokenHash.Trim();
        TokenHash = tokenHash.Trim();
        PaymentUrl = null;
        SentToEmail = sentToEmail.Trim();
        TokenProvisionedAt = now;
        SentAt = now;
        LinkStatus = ManualPaymentConstants.AccessActive;
        Version++;
        UpdatedAt = now;
    }

    public bool Allows(string action, DateTimeOffset now) =>
        LinkStatus == ManualPaymentConstants.AccessActive && RevokedAt is null && ExpiresAt > now &&
        Purpose == ManualPaymentConstants.AccessPurpose &&
        AllowedActions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(action, StringComparer.OrdinalIgnoreCase);

    public void RecordAccess(DateTimeOffset now)
    {
        LastAccessedAt = now;
        UpdatedAt = now;
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
        LinkStatus = ManualPaymentConstants.AccessRevoked;
        Version++;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
