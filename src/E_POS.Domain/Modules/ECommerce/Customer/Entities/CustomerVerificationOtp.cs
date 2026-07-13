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
}
