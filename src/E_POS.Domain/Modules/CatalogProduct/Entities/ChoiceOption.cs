using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ChoiceOption : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public Guid ChoiceGroupId { get; protected set; }
    public string OptionCode { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
}
