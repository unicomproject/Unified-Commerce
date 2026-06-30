using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.TenantFoundation.Entities;

public class Currency : AuditableEntity
{
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public int DecimalPlaces { get; protected set; }
}
