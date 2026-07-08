using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class BrandRequestValidator : IBrandRequestValidator
{
    public ApplicationError? ValidateCreate(BrandCreateRequest request)
    {
        return Validate(request.BrandCode, request.Name, request.Status);
    }

    public ApplicationError? ValidateUpdate(BrandUpdateRequest request)
    {
        return Validate(request.BrandCode, request.Name, request.Status);
    }

    private static ApplicationError? Validate(string brandCode, string name, string status)
    {
        if (string.IsNullOrWhiteSpace(brandCode)) return ValidationFailed("Brand code is required.");
        if (brandCode.Trim().Length > 80) return ValidationFailed("Brand code cannot exceed 80 characters.");
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Brand name is required.");
        if (name.Trim().Length > 200) return ValidationFailed("Brand name cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(status) || !BrandConstants.IsValidWriteStatus(status)) return ValidationFailed("Brand status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message)
    {
        return new ApplicationError("brand.validation_failed", message);
    }
}

