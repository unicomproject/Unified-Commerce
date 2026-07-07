using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class ReturnPolicyRequestValidator : IReturnPolicyRequestValidator
{
    public ApplicationError? ValidateCreate(ReturnPolicyCreateRequest request) => Validate(request.PolicyCode, request.Name, request.ReturnWindowDays, request.Status);
    public ApplicationError? ValidateUpdate(ReturnPolicyUpdateRequest request) => Validate(request.PolicyCode, request.Name, request.ReturnWindowDays, request.Status);

    private static ApplicationError? Validate(string code, string name, int? returnWindowDays, string status)
    {
        if (string.IsNullOrWhiteSpace(code)) return ValidationFailed("Return policy code is required.");
        if (code.Trim().Length > 80) return ValidationFailed("Return policy code cannot exceed 80 characters.");
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Return policy name is required.");
        if (name.Trim().Length > 200) return ValidationFailed("Return policy name cannot exceed 200 characters.");
        if (returnWindowDays is < 0) return ValidationFailed("Return window days must be greater than or equal to zero.");
        if (string.IsNullOrWhiteSpace(status) || !ReturnPolicyConstants.IsValidWriteStatus(status)) return ValidationFailed("Return policy status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("return_policies.validation_failed", message);
}

