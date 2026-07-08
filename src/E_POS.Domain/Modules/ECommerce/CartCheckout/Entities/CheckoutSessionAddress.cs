using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class CheckoutSessionAddress : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CheckoutSessionId { get; protected set; }
    public string AddressType { get; protected set; } = string.Empty;
    public string? ContactName { get; protected set; }
    public string? ContactPhone { get; protected set; }
    public string AddressLine1 { get; protected set; } = string.Empty;
    public string? AddressLine2 { get; protected set; }
    public string? City { get; protected set; }
    public string? StateOrProvince { get; protected set; }
    public string? PostalCode { get; protected set; }
    public string CountryCode { get; protected set; } = string.Empty;
}
