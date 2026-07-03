using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Application.Modules.CatalogProduct.Validators;

public sealed class DepartmentRequestValidator : IDepartmentRequestValidator
{
    public ApplicationError? ValidateCreate(DepartmentCreateRequest request) => Validate(request.DepartmentCode, request.Name, request.Status);

    public ApplicationError? ValidateUpdate(DepartmentUpdateRequest request) => Validate(request.DepartmentCode, request.Name, request.Status);

    private static ApplicationError? Validate(string departmentCode, string name, string status)
    {
        if (string.IsNullOrWhiteSpace(departmentCode) || departmentCode.Trim().Length > 80) return ValidationFailed("Department code is required and must be 80 characters or less.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return ValidationFailed("Department name is required and must be 200 characters or less.");
        if (string.IsNullOrWhiteSpace(status) || !DepartmentConstants.IsValidWriteStatus(status)) return ValidationFailed("Department status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("department.validation_failed", message);
}