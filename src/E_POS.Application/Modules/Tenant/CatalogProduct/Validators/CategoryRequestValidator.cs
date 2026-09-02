using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class CategoryRequestValidator : ICategoryRequestValidator
{
    public ApplicationError? ValidateCreate(CategoryCreateRequest request) =>
        Validate(request.CategoryCode, request.Name, request.CategorySlug, request.Description, request.Status, request.SortOrder);

    public ApplicationError? ValidateUpdate(CategoryUpdateRequest request) =>
        Validate(request.CategoryCode, request.Name, request.CategorySlug, request.Description, request.Status, request.SortOrder);

    public ApplicationError? ValidateList(CategoryListQuery query)
    {
        if (query.RootOnly && query.ParentCategoryId.HasValue)
        {
            return ValidationFailed("rootOnly cannot be combined with parentCategoryId.");
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && !CategoryConstants.IsValidManagementFilterStatus(query.Status))
        {
            return ValidationFailed("Category status filter must be ACTIVE or INACTIVE.");
        }

        return null;
    }

    public ApplicationError? ValidateTreeStatus(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status) && !CategoryConstants.IsValidManagementFilterStatus(status))
        {
            return ValidationFailed("Category status filter must be ACTIVE or INACTIVE.");
        }

        return null;
    }

    private static ApplicationError? Validate(
        string categoryCode,
        string name,
        string? categorySlug,
        string? description,
        string status,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(categoryCode) || categoryCode.Trim().Length > CategoryConstants.MaxCodeLength)
        {
            return ValidationFailed($"Category code is required and must be {CategoryConstants.MaxCodeLength} characters or less.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > CategoryConstants.MaxNameLength)
        {
            return ValidationFailed($"Category name is required and must be {CategoryConstants.MaxNameLength} characters or less.");
        }

        if (!string.IsNullOrWhiteSpace(categorySlug) && categorySlug.Trim().Length > CategoryConstants.MaxSlugLength)
        {
            return ValidationFailed($"Category slug must be {CategoryConstants.MaxSlugLength} characters or less.");
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > CategoryConstants.MaxDescriptionLength)
        {
            return ValidationFailed($"Category description must be {CategoryConstants.MaxDescriptionLength} characters or less.");
        }

        if (string.IsNullOrWhiteSpace(status) || !CategoryConstants.IsValidWriteStatus(status))
        {
            return ValidationFailed("Category status must be ACTIVE or INACTIVE.");
        }

        if (sortOrder < 0)
        {
            return ValidationFailed("Category sort order must be zero or greater.");
        }

        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("category.validation_failed", message);
}
