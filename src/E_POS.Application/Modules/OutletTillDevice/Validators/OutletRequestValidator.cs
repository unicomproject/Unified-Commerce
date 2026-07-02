using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.OutletTillDevice.Validators;

public sealed class OutletRequestValidator : IOutletRequestValidator
{
    public ApplicationError? ValidateCreate(OutletCreateRequest request)
    {
        return ValidateWriteRequest(
            request.Name,
            request.Status,
            request.OutletType,
            request.ContactPhone,
            request.ContactEmail,
            request.Address,
            request.BusinessHours,
            allowDeletedStatus: false);
    }

    public ApplicationError? ValidateUpdate(OutletUpdateRequest request)
    {
        return ValidateWriteRequest(
            request.Name,
            request.Status,
            request.OutletType,
            request.ContactPhone,
            request.ContactEmail,
            request.Address,
            request.BusinessHours,
            allowDeletedStatus: true);
    }

    private static ApplicationError? ValidateWriteRequest(
        string name,
        string status,
        string outletType,
        string? contactPhone,
        string? contactEmail,
        OutletAddressRequest? address,
        IReadOnlyList<OutletBusinessHourRequest>? businessHours,
        bool allowDeletedStatus)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return ValidationFailed("Outlet name is required and must be 200 characters or less.");
        if (string.IsNullOrWhiteSpace(status) || (allowDeletedStatus ? !OutletConstants.IsValidStatus(status) : !OutletConstants.IsValidWriteStatus(status))) return ValidationFailed(allowDeletedStatus ? "Outlet status must be ACTIVE, INACTIVE, or DELETED." : "Outlet status must be ACTIVE or INACTIVE.");
        if (string.IsNullOrWhiteSpace(outletType) || !OutletConstants.IsValidOutletType(outletType)) return ValidationFailed("Outlet type must be STORE or WAREHOUSE.");
        if (!string.IsNullOrWhiteSpace(contactPhone) && contactPhone.Trim().Length > 40) return ValidationFailed("Contact phone must be 40 characters or less.");
        if (!string.IsNullOrWhiteSpace(contactEmail) && (contactEmail.Trim().Length > 255 || !contactEmail.Contains('@', StringComparison.Ordinal))) return ValidationFailed("Contact email must be a valid email address and 255 characters or less.");

        var addressError = ValidateAddress(address);
        return addressError ?? ValidateBusinessHours(businessHours);
    }

    private static ApplicationError? ValidateAddress(OutletAddressRequest? address)
    {
        if (address is null) return ValidationFailed("Physical address is required.");
        if (string.IsNullOrWhiteSpace(address.AddressLine1) || address.AddressLine1.Trim().Length > 255) return ValidationFailed("Address line 1 is required and must be 255 characters or less.");
        if (!string.IsNullOrWhiteSpace(address.AddressLine2) && address.AddressLine2.Trim().Length > 255) return ValidationFailed("Address line 2 must be 255 characters or less.");
        if (string.IsNullOrWhiteSpace(address.City) || address.City.Trim().Length > 120) return ValidationFailed("City is required and must be 120 characters or less.");
        if (!string.IsNullOrWhiteSpace(address.StateOrProvince) && address.StateOrProvince.Trim().Length > 120) return ValidationFailed("State or province must be 120 characters or less.");
        if (!string.IsNullOrWhiteSpace(address.PostalCode) && address.PostalCode.Trim().Length > 30) return ValidationFailed("Postal code must be 30 characters or less.");
        if (string.IsNullOrWhiteSpace(address.CountryCode) || address.CountryCode.Trim().Length != 2) return ValidationFailed("Country code is required and must be 2 characters.");
        return null;
    }

    private static ApplicationError? ValidateBusinessHours(IReadOnlyList<OutletBusinessHourRequest>? businessHours)
    {
        if (businessHours is null || businessHours.Count == 0) return null;

        var days = new HashSet<int>();
        foreach (var hour in businessHours)
        {
            if (hour.DayOfWeek is < 0 or > 6) return ValidationFailed("Business hour dayOfWeek must be between 0 and 6.");
            if (!days.Add(hour.DayOfWeek)) return ValidationFailed("Business hours can contain only one entry per day.");
            if (hour.OpenTime >= hour.CloseTime) return ValidationFailed("Business hour openTime must be before closeTime.");
        }

        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("outlet.validation_failed", message);
}
