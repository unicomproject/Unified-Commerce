using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;

public sealed class OutletRequestValidator : IOutletRequestValidator
{
    private static readonly HashSet<string> SupportedCountryCodes = TenantCreateWizardReferenceData.CountryCodes
        .Select(item => item.Code)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
            allowDeletedStatus: false);
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
        var fieldErrors = new List<ApplicationFieldError>();

        if (string.IsNullOrWhiteSpace(outletName))
        {
            fieldErrors.Add(new ApplicationFieldError("outletName", "Outlet name is required."));
        }
        else if (outletName.Trim().Length > 200)
        {
            fieldErrors.Add(new ApplicationFieldError("outletName", "Outlet name must be 200 characters or less."));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            fieldErrors.Add(new ApplicationFieldError("status", "Status is required."));
        }
        else if (allowDeletedStatus ? !OutletConstants.IsValidStatus(status) : !OutletConstants.IsValidWriteStatus(status))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "status",
                allowDeletedStatus
                    ? "Outlet status must be ACTIVE, INACTIVE, or DELETED."
                    : "Outlet status must be ACTIVE or INACTIVE."));
        }

        if (string.IsNullOrWhiteSpace(outletType))
        {
            fieldErrors.Add(new ApplicationFieldError("outletType", "Outlet type is required."));
        }
        else if (!OutletConstants.IsValidOutletType(outletType))
        {
            fieldErrors.Add(new ApplicationFieldError("outletType", "Outlet type must be STORE or WAREHOUSE."));
        }

        if (string.IsNullOrWhiteSpace(timezone))
        {
            fieldErrors.Add(new ApplicationFieldError("timezone", "Timezone is required."));
        }
        else if (timezone.Trim().Length > 80)
        {
            fieldErrors.Add(new ApplicationFieldError("timezone", "Timezone must be 80 characters or less."));
        }
        else if (!IsValidTimezone(timezone))
        {
            fieldErrors.Add(new ApplicationFieldError("timezone", "Timezone must be a valid IANA timezone."));
        }

        if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length > 40)
        {
            fieldErrors.Add(new ApplicationFieldError("phone", "Phone must be 40 characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Trim().Length > 255)
            {
                fieldErrors.Add(new ApplicationFieldError("email", "Email must be 255 characters or less."));
            }
            else if (!email.Contains('@', StringComparison.Ordinal))
            {
                fieldErrors.Add(new ApplicationFieldError("email", "Email must be a valid email address."));
            }
        }

        ValidateAddress(address, fieldErrors);
        ValidateBusinessHours(businessHours, fieldErrors);

        return fieldErrors.Count == 0
            ? null
            : new ApplicationError("outlet.validation_failed", "Outlet validation failed.", fieldErrors);
    }

    private static void ValidateAddress(OutletAddressRequest? address, ICollection<ApplicationFieldError> fieldErrors)
    {
        if (address is null)
        {
            fieldErrors.Add(new ApplicationFieldError("address", "Physical address is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(address.AddressLine1))
        {
            fieldErrors.Add(new ApplicationFieldError("address.addressLine1", "Address line 1 is required."));
        }
        else if (address.AddressLine1.Trim().Length > 255)
        {
            fieldErrors.Add(new ApplicationFieldError("address.addressLine1", "Address line 1 must be 255 characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(address.AddressLine2) && address.AddressLine2.Trim().Length > 255)
        {
            fieldErrors.Add(new ApplicationFieldError("address.addressLine2", "Address line 2 must be 255 characters or less."));
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            fieldErrors.Add(new ApplicationFieldError("address.city", "City is required."));
        }
        else if (address.City.Trim().Length > 120)
        {
            fieldErrors.Add(new ApplicationFieldError("address.city", "City must be 120 characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(address.StateOrProvince) && address.StateOrProvince.Trim().Length > 120)
        {
            fieldErrors.Add(new ApplicationFieldError("address.stateOrProvince", "State or province must be 120 characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(address.PostalCode) && address.PostalCode.Trim().Length > 30)
        {
            fieldErrors.Add(new ApplicationFieldError("address.postalCode", "Postal code must be 30 characters or less."));
        }

        if (string.IsNullOrWhiteSpace(address.CountryCode))
        {
            fieldErrors.Add(new ApplicationFieldError("address.countryCode", "Country code is required."));
        }
        else if (address.CountryCode.Trim().Length != 2)
        {
            fieldErrors.Add(new ApplicationFieldError("address.countryCode", "Country code must be exactly 2 characters."));
        }
        else if (!SupportedCountryCodes.Contains(address.CountryCode.Trim()))
        {
            fieldErrors.Add(new ApplicationFieldError("address.countryCode", "Country code is not supported."));
        }

        if (!string.IsNullOrWhiteSpace(address.ContactName) && address.ContactName.Trim().Length > 150)
        {
            fieldErrors.Add(new ApplicationFieldError("address.contactName", "Contact name must be 150 characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(address.ContactPhone) && address.ContactPhone.Trim().Length > 40)
        {
            fieldErrors.Add(new ApplicationFieldError("address.contactPhone", "Contact phone must be 40 characters or less."));
        }
    }

    private static void ValidateBusinessHours(IReadOnlyList<OutletBusinessHourRequest>? businessHours, ICollection<ApplicationFieldError> fieldErrors)
    {
        if (businessHours is null || businessHours.Count == 0)
        {
            return;
        }

        var days = new HashSet<int>();
        for (var index = 0; index < businessHours.Count; index++)
        {
            var hour = businessHours[index];
            var prefix = $"businessHours[{index}]";

            if (hour.DayOfWeek is < 0 or > 6)
            {
                fieldErrors.Add(new ApplicationFieldError($"{prefix}.dayOfWeek", "dayOfWeek must be between 0 and 6."));
            }
            else if (!days.Add(hour.DayOfWeek))
            {
                fieldErrors.Add(new ApplicationFieldError($"{prefix}.dayOfWeek", "Business hours can contain only one entry per day."));
            }

            if (hour.IsClosed)
            {
                if (hour.OpeningTime.HasValue)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.openingTime", "Opening time must be null when the day is closed."));
                }

                if (hour.ClosingTime.HasValue)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.closingTime", "Closing time must be null when the day is closed."));
                }
            }
            else
            {
                if (!hour.OpeningTime.HasValue)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.openingTime", "Opening time is required when the outlet is open."));
                }

                if (!hour.ClosingTime.HasValue)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.closingTime", "Closing time is required when the outlet is open."));
                }

                if (hour.OpeningTime.HasValue && hour.ClosingTime.HasValue && hour.OpeningTime >= hour.ClosingTime)
                {
                    fieldErrors.Add(new ApplicationFieldError($"{prefix}.closingTime", "Closing time must be later than opening time."));
                }
            }

            if (hour.ValidFrom.HasValue && hour.ValidUntil.HasValue && hour.ValidUntil < hour.ValidFrom)
            {
                fieldErrors.Add(new ApplicationFieldError($"{prefix}.validUntil", "validUntil must be on or after validFrom."));
            }
        }
    }

    private static bool IsValidTimezone(string timezone)
    {
        var normalized = timezone.Trim();
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalized);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return TryResolveConvertedTimezone(normalized);
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool TryResolveConvertedTimezone(string timezone)
    {
        if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(timezone, out var windowsTimezoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(windowsTimezoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
