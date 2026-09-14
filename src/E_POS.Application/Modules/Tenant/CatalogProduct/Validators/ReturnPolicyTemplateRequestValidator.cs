using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public sealed class ReturnPolicyTemplateRequestValidator : IReturnPolicyTemplateRequestValidator
{
    public ApplicationError? ValidateCreate(ReturnPolicyTemplateCreateRequest request) =>
        Validate(request.TemplateCode, request.Name, request.Description, request.ReturnWindowDays, request.ExchangeWindowDays, request.Status);

    public ApplicationError? ValidateUpdate(ReturnPolicyTemplateUpdateRequest request) =>
        Validate(request.TemplateCode, request.Name, request.Description, request.ReturnWindowDays, request.ExchangeWindowDays, request.Status);

    private static ApplicationError? Validate(string code, string name, string? description, int? returnWindowDays, int? exchangeWindowDays, string status)
    {
        if (string.IsNullOrWhiteSpace(code)) return ValidationFailed("Return policy template code is required.");
        if (code.Trim().Length > 80) return ValidationFailed("Return policy template code cannot exceed 80 characters.");
        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Return policy template name is required.");
        if (name.Trim().Length > 200) return ValidationFailed("Return policy template name cannot exceed 200 characters.");
        if (description?.Trim().Length > 2000) return ValidationFailed("Return policy template description cannot exceed 2000 characters.");
        if (returnWindowDays is < 0) return ValidationFailed("Return window days must be greater than or equal to zero.");
        if (exchangeWindowDays is < 0) return ValidationFailed("Exchange window days must be greater than or equal to zero.");
        if (string.IsNullOrWhiteSpace(status) || !ReturnPolicyTemplateConstants.IsValidWriteStatus(status)) return ValidationFailed("Return policy template status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("return_policy_templates.validation_failed", message);
}

