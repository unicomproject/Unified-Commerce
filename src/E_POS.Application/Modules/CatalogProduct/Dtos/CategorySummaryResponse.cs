namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CategorySummaryResponse(
    Guid Id,
    string CategoryCode,
    string CategoryName,
    string Status,
    Guid? ParentCategoryId,
    string? ParentCategoryCode,
    string? ParentCategoryName,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);