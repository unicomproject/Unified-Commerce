using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerPasswordResetToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerAuthAccountId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public Guid? VerifiedOtpId { get; protected set; }
}
