namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string? ParentCategoryCode,
    string? ParentCategoryName,
    string CategoryCode,
    string CategoryName,
    string CategorySlug,
    string? Description,
    Guid? ImageMediaAssetId,
    string? ImageUrl,
    string Status,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int Level,
    string HierarchyPath,
    int ChildCount,
    int ProductCount,
    bool HasChildren);
