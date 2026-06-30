using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ChoiceGroup : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string ChoiceGroupCode { get; protected set; } = string.Empty;
    public int MaxSelect { get; protected set; }
    public int MinSelect { get; protected set; }
}
