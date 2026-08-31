namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryListQuery(
    int PageNumber = 1,
    int PageSize = 50,
    string? Search = null,
    string? Status = null,
    Guid? ParentCategoryId = null,
    bool RootOnly = false);
