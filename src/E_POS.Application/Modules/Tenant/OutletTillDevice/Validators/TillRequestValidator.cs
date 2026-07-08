using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;

public sealed class TillRequestValidator : ITillRequestValidator
{
    public ApplicationError? ValidateCreate(TillCreateRequest request)
    {
        return ValidateWriteRequest(request);
    }

    public ApplicationError? ValidateUpdate(TillUpdateRequest request)
    {
        return ValidateWriteRequest(request);
    }

    private static ApplicationError? ValidateWriteRequest(TillCreateRequest request)
    {
        if (request.OutletId == Guid.Empty) return ValidationFailed("Outlet is required.");
        if (string.IsNullOrWhiteSpace(request.TillName) || request.TillName.Trim().Length > 150) return ValidationFailed("Till name is required and must be 150 characters or less.");
        if (string.IsNullOrWhiteSpace(request.TillCode) || request.TillCode.Trim().Length > 60) return ValidationFailed("Till code is required and must be 60 characters or less.");
        if (string.IsNullOrWhiteSpace(request.TillType) || request.TillType.Trim().Length > 40) return ValidationFailed("Till type is required and must be 40 characters or less.");
        if (request.DefaultOpeningFloatAmount < 0) return ValidationFailed("Default opening float amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(request.CurrencyCode) || request.CurrencyCode.Trim().Length != 3) return ValidationFailed("Currency code is required and must be 3 characters.");
        if (string.IsNullOrWhiteSpace(request.Status) || !TillConstants.IsValidWriteStatus(request.Status)) return ValidationFailed("Till status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError? ValidateWriteRequest(TillUpdateRequest request)
    {
        if (request.OutletId == Guid.Empty) return ValidationFailed("Outlet is required.");
        if (string.IsNullOrWhiteSpace(request.TillName) || request.TillName.Trim().Length > 150) return ValidationFailed("Till name is required and must be 150 characters or less.");
        if (string.IsNullOrWhiteSpace(request.TillCode) || request.TillCode.Trim().Length > 60) return ValidationFailed("Till code is required and must be 60 characters or less.");
        if (string.IsNullOrWhiteSpace(request.TillType) || request.TillType.Trim().Length > 40) return ValidationFailed("Till type is required and must be 40 characters or less.");
        if (request.DefaultOpeningFloatAmount < 0) return ValidationFailed("Default opening float amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(request.CurrencyCode) || request.CurrencyCode.Trim().Length != 3) return ValidationFailed("Currency code is required and must be 3 characters.");
        if (string.IsNullOrWhiteSpace(request.Status) || !TillConstants.IsValidWriteStatus(request.Status)) return ValidationFailed("Till status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("till.validation_failed", message);
}
