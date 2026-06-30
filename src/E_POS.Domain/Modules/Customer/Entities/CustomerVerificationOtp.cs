using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerVerificationOtp : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public int AttemptCount { get; protected set; }
    public int MaxAttempts { get; protected set; }
    public string NormalizedRecipientValue { get; protected set; } = string.Empty;
    public string VerificationPurpose { get; protected set; } = string.Empty;
}
