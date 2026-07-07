using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;

public sealed class PosDeviceRequestValidator : IPosDeviceRequestValidator
{
    public ApplicationError? ValidateCreate(PosDeviceCreateRequest request)
    {
        return ValidateWriteRequest(request.OutletId, request.Name, request.DeviceSerialNumber, request.Status);
    }

    public ApplicationError? ValidateUpdate(PosDeviceUpdateRequest request)
    {
        return ValidateWriteRequest(request.OutletId, request.Name, request.DeviceSerialNumber, request.Status);
    }

    private static ApplicationError? ValidateWriteRequest(Guid outletId, string name, string deviceSerialNumber, string status)
    {
        if (outletId == Guid.Empty) return ValidationFailed("Outlet is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return ValidationFailed("POS device name is required and must be 200 characters or less.");
        if (string.IsNullOrWhiteSpace(deviceSerialNumber) || deviceSerialNumber.Trim().Length > 80) return ValidationFailed("POS device serial number is required and must be 80 characters or less.");
        if (string.IsNullOrWhiteSpace(status) || !PosDeviceConstants.IsValidWriteStatus(status)) return ValidationFailed("POS device status must be ACTIVE or INACTIVE.");
        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("pos_device.validation_failed", message);
}

