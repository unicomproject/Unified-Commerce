using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ReturnPolicy : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string PolicyCode { get; protected set; } = string.Empty;
    public int? ReturnWindowDays { get; protected set; }
}
