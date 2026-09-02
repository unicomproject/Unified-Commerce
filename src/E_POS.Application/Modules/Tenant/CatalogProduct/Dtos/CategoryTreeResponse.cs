namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryTreeResponse(IReadOnlyList<CategoryTreeNodeResponse> Items);
