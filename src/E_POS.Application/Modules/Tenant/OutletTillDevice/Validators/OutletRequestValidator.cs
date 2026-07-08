using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;

public sealed class OutletRequestValidator : IOutletRequestValidator
{
    public ApplicationError? ValidateCreate(OutletCreateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletName,
            request.Status,
            request.OutletType,
            request.Timezone,
            request.Phone,
            request.Email,
            request.Address,
            request.BusinessHours,
            allowDeletedStatus: false);
    }

    public ApplicationError? ValidateUpdate(OutletUpdateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletName,
            request.Status,
            request.OutletType,
            request.Timezone,
            request.Phone,
            request.Email,
            request.Address,
            request.BusinessHours,
            allowDeletedStatus: true);
    }

    private static ApplicationError? ValidateWriteRequest(
        string outletName,
        string status,
        string outletType,
        string timezone,
        string? phone,
        string? email,
        OutletAddressRequest? address,
        IReadOnlyList<OutletBusinessHourRequest>? businessHours,
        bool allowDeletedStatus)
    {
        if (string.IsNullOrWhiteSpace(outletName) || outletName.Trim().Length > 200) return ValidationFailed("Outlet name is required and must be 200 characters or less.");
        if (string.IsNullOrWhiteSpace(status) || (allowDeletedStatus ? !OutletConstants.IsValidStatus(status) : !OutletConstants.IsValidWriteStatus(status))) return ValidationFailed(allowDeletedStatus ? "Outlet status must be ACTIVE, INACTIVE, or DELETED." : "Outlet status must be ACTIVE or INACTIVE.");
        if (string.IsNullOrWhiteSpace(outletType) || !OutletConstants.IsValidOutletType(outletType)) return ValidationFailed("Outlet type must be STORE or WAREHOUSE.");
        if (string.IsNullOrWhiteSpace(timezone) || timezone.Trim().Length > 80) return ValidationFailed("Timezone is required and must be 80 characters or less.");
        if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length > 40) return ValidationFailed("Phone must be 40 characters or less.");
        if (!string.IsNullOrWhiteSpace(email) && (email.Trim().Length > 255 || !email.Contains('@', StringComparison.Ordinal))) return ValidationFailed("Email must be a valid email address and 255 characters or less.");

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
        if (!string.IsNullOrWhiteSpace(address.ContactName) && address.ContactName.Trim().Length > 150) return ValidationFailed("Contact name must be 150 characters or less.");
        if (!string.IsNullOrWhiteSpace(address.ContactPhone) && address.ContactPhone.Trim().Length > 40) return ValidationFailed("Contact phone must be 40 characters or less.");
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
            if (!hour.IsClosed && (!hour.OpeningTime.HasValue || !hour.ClosingTime.HasValue)) return ValidationFailed("Opening and closing times are required when the outlet is open.");
            if (!hour.IsClosed && hour.OpeningTime >= hour.ClosingTime) return ValidationFailed("Business hour openingTime must be before closingTime.");
            if (hour.ValidFrom.HasValue && hour.ValidUntil.HasValue && hour.ValidUntil < hour.ValidFrom) return ValidationFailed("Business hour validUntil must be on or after validFrom.");
        }

        return null;
    }

    private static ApplicationError ValidationFailed(string message) => new("outlet.validation_failed", message);
}
