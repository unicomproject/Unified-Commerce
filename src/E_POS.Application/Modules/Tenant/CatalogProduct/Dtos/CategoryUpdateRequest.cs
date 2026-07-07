namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryUpdateRequest(
    Guid DepartmentId,
    string CategoryCode, 
    string Name, 
    string? CategorySlug,
    string? Description,
    string Status, 
    Guid? ParentCategoryId, 
    int SortOrder);
