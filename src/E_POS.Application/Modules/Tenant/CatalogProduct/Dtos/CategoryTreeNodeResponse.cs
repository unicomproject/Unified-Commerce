namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryTreeNodeResponse(
    Guid Id,
    string CategoryCode,
    string CategoryName,
    string Status,
    Guid? ParentCategoryId,
    int SortOrder,
    int Level,
    string HierarchyPath,
    int ChildCount,
    int ProductCount,
    bool HasChildren,
    IReadOnlyList<CategoryTreeNodeResponse> Children);
