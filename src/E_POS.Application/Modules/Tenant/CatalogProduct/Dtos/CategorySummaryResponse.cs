namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CategorySummaryResponse(
    Guid Id,
    string CategoryCode,
    string CategoryName,
    string? ImageUrl,
    string Status,
    Guid? ParentCategoryId,
    string? ParentCategoryCode,
    string? ParentCategoryName,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
