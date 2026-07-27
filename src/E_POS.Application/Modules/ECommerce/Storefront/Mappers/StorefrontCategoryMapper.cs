using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Mappers;

public static class StorefrontCategoryMapper
{
    public static StorefrontCategoryReadModel ToReadModel(this Category category, string? mediaPublicUrl = null)
    {
        return new StorefrontCategoryReadModel
        {
            Id = category.Id,
            Name = category.CategoryName,
            ImageUrl = mediaPublicUrl ?? string.Empty
        };
    }

    public static IEnumerable<StorefrontCategoryReadModel> ToReadModels(this IEnumerable<Category> categories)
    {
        return categories.Select(category => category.ToReadModel());
    }

    public static StorefrontCategoryListReadModel ToListReadModel(this Category category, int itemCount, string? mediaPublicUrl = null)
    {
        return new StorefrontCategoryListReadModel
        {
            Id = category.Id,
            Name = category.CategoryName,
            Slug = category.CategorySlug,
            Description = category.Description ?? string.Empty,
            ImageUrl = mediaPublicUrl ?? string.Empty,
            ItemCount = itemCount,
            SortOrder = category.SortOrder
        };
    }
}