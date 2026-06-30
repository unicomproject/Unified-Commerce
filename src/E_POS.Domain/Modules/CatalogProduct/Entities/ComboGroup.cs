using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboGroup : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public Guid ComboDefinitionId { get; protected set; }
    public string GroupCode { get; protected set; } = string.Empty;
    public int MaxSelect { get; protected set; }
    public int MinSelect { get; protected set; }
}
