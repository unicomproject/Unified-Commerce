using System.Net;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerConsent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerId { get; protected set; }
    public string ConsentType { get; protected set; } = string.Empty;
    public Guid? SalesChannelId { get; protected set; }
    public string? PolicyVersion { get; protected set; }
    public string ConsentStatus { get; protected set; } = string.Empty;
    public string ConsentSource { get; protected set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; protected set; }
    public DateTimeOffset? WithdrawnAt { get; protected set; }
    public IPAddress? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }

    protected CustomerConsent() { }

    public static CustomerConsent Grant(
        Guid id,
        Guid tenantId,
        Guid customerId,
        string consentType,
        Guid? salesChannelId,
        string? policyVersion,
        string consentSource,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        return new CustomerConsent
        {
            Id = id,
            TenantId = tenantId,
            CustomerId = customerId,
            ConsentType = consentType.Trim().ToUpperInvariant(),
            SalesChannelId = salesChannelId,
            PolicyVersion = string.IsNullOrWhiteSpace(policyVersion) ? null : policyVersion.Trim(),
            ConsentStatus = "GRANTED",
            ConsentSource = consentSource.Trim().ToUpperInvariant(),
            RecordedAt = now,
            IpAddress = ipAddress,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}