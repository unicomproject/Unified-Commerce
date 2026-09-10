using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class BrandRequestValidator : IBrandRequestValidator
{
    public ApplicationError? ValidateCreate(BrandCreateRequest request)
    {
        return Validate(request.BrandCode, request.Name, request.Description, request.Status, request.SortOrder);
    }

    public ApplicationError? ValidateUpdate(BrandUpdateRequest request)
    {
        var error = Validate(request.BrandCode, request.Name, request.Description, request.Status, request.SortOrder);
        if (error is not null) return error;
        return request.ExpectedRowVersion < 1
            ? ValidationFailed("expectedRowVersion", "Expected row version must be at least 1.")
            : null;
    }

    private static ApplicationError? Validate(string brandCode, string name, string? description, string status, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(brandCode)) return ValidationFailed("brandCode", "Brand code is required.");
        if (brandCode.Trim().Length > 80) return ValidationFailed("brandCode", "Brand code cannot exceed 80 characters.");
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("name", "Brand name is required.");
        if (name.Trim().Length > 150) return ValidationFailed("name", "Brand name cannot exceed 150 characters.");
        if (description?.Trim().Length > 255) return ValidationFailed("description", "Brand description cannot exceed 255 characters.");
        if (sortOrder < 0) return ValidationFailed("sortOrder", "Brand sort order cannot be negative.");
        if (string.IsNullOrWhiteSpace(status) || !BrandConstants.IsValidWriteStatus(status)) return ValidationFailed("status", "Brand status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string field, string message)
    {
        return new ApplicationError("brand.validation_failed", "Brand validation failed.", [new ApplicationFieldError(field, message)]);
    }
}

