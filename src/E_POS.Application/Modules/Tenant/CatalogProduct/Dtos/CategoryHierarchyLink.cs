namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryHierarchyLink(
    Guid Id,
    Guid? ParentCategoryId,
    string CategoryCode,
    string CategoryName,
    string Status,
    int SortOrder);
