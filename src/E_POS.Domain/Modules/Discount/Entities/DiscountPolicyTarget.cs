using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicyTarget : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
    public string TargetType { get; protected set; } = string.Empty;
    public string TargetMode { get; protected set; } = string.Empty;
    public Guid? ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? CategoryId { get; protected set; }
    public Guid? BrandId { get; protected set; }
    public Guid? CollectionId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}