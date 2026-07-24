namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategoryResponse(
    Guid Id,
    string CategoryCode,
    string CategoryName,
    string? ImageUrl,
    Guid? ImageMediaAssetId,
    string Status,
    Guid? ParentCategoryId,
    string? ParentCategoryCode,
    string? ParentCategoryName,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
