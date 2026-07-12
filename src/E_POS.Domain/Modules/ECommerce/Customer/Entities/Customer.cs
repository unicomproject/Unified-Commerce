using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class Customer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty; // Maps to display_name
    public string NormalizedEmail { get; protected set; } = string.Empty;
    public string NormalizedPhone { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string CustomerCode { get; protected set; } = string.Empty;
    public string? FirstName { get; protected set; }
    public string? LastName { get; protected set; }
    public string? Email { get; protected set; }
    public string? Phone { get; protected set; }
    public string SourceType { get; protected set; } = string.Empty;
    public Guid? SourceSalesChannelId { get; protected set; }
    public DateTimeOffset? AnonymizedAt { get; protected set; }
}
