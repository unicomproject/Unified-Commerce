using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerVerificationOtp : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public int AttemptCount { get; protected set; }
    public int MaxAttempts { get; protected set; }
    public string NormalizedRecipientValue { get; protected set; } = string.Empty;
    public string VerificationPurpose { get; protected set; } = string.Empty;
    public string DeliveryChannel { get; protected set; } = string.Empty;
    public string RecipientValue { get; protected set; } = string.Empty;
    public string? OtpHash { get; protected set; }
    public int ResendCount { get; protected set; }
    public DateTimeOffset SentAt { get; protected set; }
    public DateTimeOffset? LastSentAt { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? VerifiedAt { get; protected set; }
    public DateTimeOffset? InvalidatedAt { get; protected set; }
    public string? ProviderName { get; protected set; }
    public string? ProviderMessageId { get; protected set; }
    public IPAddress? RequestIpAddress { get; protected set; }
    public string? RequestUserAgent { get; protected set; }

    protected CustomerVerificationOtp() { }

    public static CustomerVerificationOtp Create(
        Guid id,
        Guid tenantId,
        Guid? customerId,
        string verificationPurpose,
        string deliveryChannel,
        string recipientValue,
        string normalizedRecipientValue,
        string otpHash,
        int maxAttempts,
        DateTimeOffset sentAt,
        DateTimeOffset expiresAt,
        IPAddress? requestIpAddress,
        string? requestUserAgent)
    {
        return new CustomerVerificationOtp
        {
            Id = id,
            TenantId = tenantId,
            CustomerId = customerId,
            VerificationPurpose = verificationPurpose.Trim().ToUpperInvariant(),
            DeliveryChannel = deliveryChannel.Trim().ToUpperInvariant(),
            RecipientValue = recipientValue.Trim(),
            NormalizedRecipientValue = normalizedRecipientValue.Trim().ToUpperInvariant(),
            OtpHash = otpHash,
            Status = "PENDING",
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            ResendCount = 0,
            SentAt = sentAt,
            LastSentAt = sentAt,
            ExpiresAt = expiresAt,
            RequestIpAddress = requestIpAddress,
            RequestUserAgent = string.IsNullOrWhiteSpace(requestUserAgent) ? null : requestUserAgent.Trim(),
            CreatedAt = sentAt,
            UpdatedAt = sentAt
        };
    }

    public bool IsPending(DateTimeOffset now) =>
        string.Equals(Status, "PENDING", StringComparison.OrdinalIgnoreCase) &&
        InvalidatedAt is null &&
        VerifiedAt is null &&
        ExpiresAt > now;

    public void MarkProviderAccepted(string providerName, string? providerMessageId, DateTimeOffset now)
    {
        ProviderName = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim();
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        UpdatedAt = now;
    }

    public void MarkVerified(DateTimeOffset now)
    {
        Status = "VERIFIED";
        VerifiedAt = now;
        UpdatedAt = now;
    }

    public void RecordFailedAttempt(DateTimeOffset now)
    {
        AttemptCount++;
        if (AttemptCount >= MaxAttempts)
        {
            Status = "FAILED";
            InvalidatedAt = now;
        }

        UpdatedAt = now;
    }

    public void MarkExpired(DateTimeOffset now)
    {
        Status = "EXPIRED";
        InvalidatedAt = now;
        UpdatedAt = now;
    }

    public void Invalidate(DateTimeOffset now)
    {
        Status = "INVALIDATED";
        InvalidatedAt = now;
        UpdatedAt = now;
    }
}