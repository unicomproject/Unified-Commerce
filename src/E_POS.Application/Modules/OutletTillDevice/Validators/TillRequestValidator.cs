using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.OutletTillDevice.Validators;

public sealed class TillRequestValidator : ITillRequestValidator
{
    public ApplicationError? ValidateCreate(TillCreateRequest request)
    {
        return ValidateWriteRequest(request.OutletId, request.Name, request.TillCode, request.Status);
    }

    public ApplicationError? ValidateUpdate(TillUpdateRequest request)
    {
        return ValidateWriteRequest(request.OutletId, request.Name, request.TillCode, request.Status);
    }

    private static ApplicationError? ValidateWriteRequest(Guid outletId, string name, string tillCode, string status)
    {
        if (outletId == Guid.Empty) return ValidationFailed("Outlet is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return ValidationFailed("Till name is required and must be 200 characters or less.");
        if (string.IsNullOrWhiteSpace(tillCode) || tillCode.Trim().Length > 80) return ValidationFailed("Till code is required and must be 80 characters or less.");
        if (string.IsNullOrWhiteSpace(status) || !TillConstants.IsValidWriteStatus(status)) return ValidationFailed("Till status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("till.validation_failed", message);
}
