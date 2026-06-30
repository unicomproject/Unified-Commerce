using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class Tenant : AuditableEntity
{
    public string TenantCode { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string BaseCurrency { get; protected set; } = string.Empty;
    public string BillingStatus { get; protected set; } = string.Empty;
    public string? BusinessType { get; protected set; }
    public Guid BusinessTypeId { get; protected set; }
    public string DefaultLocale { get; protected set; } = string.Empty;
    public string DefaultTimezone { get; protected set; } = string.Empty;
    public string OperatingMode { get; protected set; } = string.Empty;
    public string PrimaryDomain { get; protected set; } = string.Empty;
}
