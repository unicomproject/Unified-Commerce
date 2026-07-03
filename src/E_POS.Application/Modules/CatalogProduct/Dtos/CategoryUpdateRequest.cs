namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CategoryUpdateRequest(string CategoryCode, string Name, string Status, Guid? ParentCategoryId, int SortOrder);