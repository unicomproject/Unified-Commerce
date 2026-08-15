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
        return Validate(request.BrandCode, request.Name, request.Description, request.Status, request.SortOrder);
    }

    private static ApplicationError? Validate(string brandCode, string name, string? description, string status, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(brandCode)) return ValidationFailed("Brand code is required.");
        if (brandCode.Trim().Length > 80) return ValidationFailed("Brand code cannot exceed 80 characters.");
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Brand name is required.");
        if (name.Trim().Length > 150) return ValidationFailed("Brand name cannot exceed 150 characters.");
        if (description?.Trim().Length > 255) return ValidationFailed("Brand description cannot exceed 255 characters.");
        if (sortOrder < 0) return ValidationFailed("Brand sort order cannot be negative.");
        if (string.IsNullOrWhiteSpace(status) || !BrandConstants.IsValidWriteStatus(status)) return ValidationFailed("Brand status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message)
    {
        return new ApplicationError("brand.validation_failed", message);
    }
}

