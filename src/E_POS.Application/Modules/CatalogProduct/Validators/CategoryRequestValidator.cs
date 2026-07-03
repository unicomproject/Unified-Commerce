using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Application.Modules.CatalogProduct.Validators;

public sealed class CategoryRequestValidator : ICategoryRequestValidator
{
    public ApplicationError? ValidateCreate(CategoryCreateRequest request) => Validate(request.CategoryCode, request.Name, request.Status, request.SortOrder);

    public ApplicationError? ValidateUpdate(CategoryUpdateRequest request) => Validate(request.CategoryCode, request.Name, request.Status, request.SortOrder);

    private static ApplicationError? Validate(string categoryCode, string name, string status, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(categoryCode) || categoryCode.Trim().Length > 40) return ValidationFailed("Category code is required and must be 40 characters or less.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return ValidationFailed("Category name is required and must be 200 characters or less.");
        if (string.IsNullOrWhiteSpace(status) || !CategoryConstants.IsValidWriteStatus(status)) return ValidationFailed("Category status must be ACTIVE or INACTIVE.");
        if (sortOrder < 0) return ValidationFailed("Category sort order must be zero or greater.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("category.validation_failed", message);
}