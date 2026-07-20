using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Mappers;

public static class StorefrontCategoryMapper
{
    public static StorefrontCategoryReadModel ToReadModel(this Category category)
    {
        return new StorefrontCategoryReadModel
        {
            Id = category.Id,
            Name = category.CategoryName,
            ImageUrl = category.ImageUrl ?? string.Empty
        };
    }

    public static IEnumerable<StorefrontCategoryReadModel> ToReadModels(this IEnumerable<Category> categories)
    {
        return categories.Select(ToReadModel);
    }

    public static StorefrontCategoryListReadModel ToListReadModel(this Category category, int itemCount)
    {
        return new StorefrontCategoryListReadModel
        {
            Id = category.Id,
            Name = category.CategoryName,
            Slug = category.CategorySlug,
            Description = category.Description ?? string.Empty,
            ImageUrl = category.ImageUrl ?? string.Empty,
            ItemCount = itemCount,
            SortOrder = category.SortOrder
        };
    }
}