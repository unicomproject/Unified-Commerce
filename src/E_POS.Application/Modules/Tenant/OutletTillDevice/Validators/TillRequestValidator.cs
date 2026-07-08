using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;

public sealed class TillRequestValidator : ITillRequestValidator
{
    public ApplicationError? ValidateCreate(TillCreateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletId,
            request.TillName,
            request.TillAreaName,
            request.TillNumber,
            request.TillCode,
            request.TillType,
            request.DefaultOpeningFloatAmount,
            request.CurrencyCode,
            request.Status);
    }

    public ApplicationError? ValidateUpdate(TillUpdateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletId,
            request.TillName,
            request.TillAreaName,
            request.TillNumber,
            request.TillCode,
            request.TillType,
            request.DefaultOpeningFloatAmount,
            request.CurrencyCode,
            request.Status);
    }

    private static ApplicationError? ValidateWriteRequest(
        Guid outletId,
        string tillName,
        string tillAreaName,
        int tillNumber,
        string tillCode,
        string tillType,
        decimal defaultOpeningFloatAmount,
        string currencyCode,
        string status)
    {
        if (outletId == Guid.Empty) return ValidationFailed("Outlet is required.");
        if (string.IsNullOrWhiteSpace(tillName) || tillName.Trim().Length > 150) return ValidationFailed("Till name is required and must be 150 characters or less.");
        if (string.IsNullOrWhiteSpace(tillAreaName) || tillAreaName.Trim().Length > 80) return ValidationFailed("Till area name is required and must be 80 characters or less.");
        if (tillNumber <= 0) return ValidationFailed("Till number must be greater than 0.");
        if (string.IsNullOrWhiteSpace(tillCode) || tillCode.Trim().Length > 60) return ValidationFailed("Till code is required and must be 60 characters or less.");
        if (string.IsNullOrWhiteSpace(tillType) || tillType.Trim().Length > 40) return ValidationFailed("Till type is required and must be 40 characters or less.");
        if (defaultOpeningFloatAmount < 0) return ValidationFailed("Default opening float amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3) return ValidationFailed("Currency code is required and must be 3 characters.");
        if (string.IsNullOrWhiteSpace(status) || !TillConstants.IsValidWriteStatus(status)) return ValidationFailed("Till status must be ACTIVE, INACTIVE, or MAINTENANCE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("till.validation_failed", message);
}
