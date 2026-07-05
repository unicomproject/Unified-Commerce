namespace E_POS.Application.Modules.PricingTax.Dtos;

public class ProductTaxAssignmentCreateRequest
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid TaxClassId { get; set; }
    public DateTimeOffset? AppliesFrom { get; set; }
    public DateTimeOffset? AppliesUntil { get; set; }
}

public class ProductTaxAssignmentUpdateRequest
{
    public Guid TaxClassId { get; set; }
    public DateTimeOffset? AppliesFrom { get; set; }
    public DateTimeOffset? AppliesUntil { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProductTaxAssignmentResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid TaxClassId { get; set; }
    public DateTimeOffset? AppliesFrom { get; set; }
    public DateTimeOffset? AppliesUntil { get; set; }
    public string Status { get; set; } = string.Empty;
}

public record ProductTaxAssignmentListResponse(
    IReadOnlyCollection<ProductTaxAssignmentResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
