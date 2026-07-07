using System.Net.Mail;
using System.Text.RegularExpressions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Validators;

public static partial class PlatformTenantCreateRequestValidator
{
    private static readonly HashSet<string> AllowedBillingStatuses =
    [
        TenantBillingStatusConstants.Pending,
        TenantBillingStatusConstants.Paid,
        TenantBillingStatusConstants.Overdue,
        TenantBillingStatusConstants.Failed,
        TenantBillingStatusConstants.Waived
    ];

    private static readonly HashSet<string> AllowedSubscriptionStatuses =
    [
        TenantSubscriptionStatusConstants.Trial,
        TenantSubscriptionStatusConstants.Active,
        TenantSubscriptionStatusConstants.PastDue,
        TenantSubscriptionStatusConstants.Cancelled,
        TenantSubscriptionStatusConstants.Expired
    ];

    private static readonly HashSet<string> AllowedPaymentMethods =
        TenantSubscriptionBillingConstants.PaymentMethods
            .Select(item => item.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static ApplicationError? ValidateWizard(CreatePlatformTenantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fieldErrors = new List<ApplicationFieldError>();

        ValidateCountryCode(fieldErrors, "countryCode", request.CountryCode, required: false);
        ValidateCurrencyCode(fieldErrors, "baseCurrency", request.BaseCurrency, required: false);

        if (request.Address is not null)
        {
            ValidateCountryCode(fieldErrors, "address.countryCode", request.Address.CountryCode, required: false);
        }

        ValidateBillingStatus(fieldErrors, request.BillingStatus);

        if (request.Subscription is not null)
        {
            ValidateSubscriptionStatus(fieldErrors, request.Subscription.SubscriptionStatus);
            ValidatePaymentMethod(fieldErrors, request.Subscription.PaymentMethod);
        }

        if (request.TenantAdmin is not null)
        {
            ValidateTenantAdmin(fieldErrors, request.TenantAdmin);
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return ApplicationError.ValidationFailed(
            "One or more tenant create fields are invalid.",
            fieldErrors);
    }

    private static void ValidateCountryCode(
        ICollection<ApplicationFieldError> fieldErrors,
        string fieldName,
        string? value,
        bool required)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (required)
            {
                fieldErrors.Add(new ApplicationFieldError(fieldName, "Country code is required."));
            }

            return;
        }

        if (!IsoCountryCodeRegex().IsMatch(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                fieldName,
                "Country code must be exactly 2 letters (for example LK)."));
        }
    }

    private static void ValidateCurrencyCode(
        ICollection<ApplicationFieldError> fieldErrors,
        string fieldName,
        string? value,
        bool required)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (required)
            {
                fieldErrors.Add(new ApplicationFieldError(fieldName, "Currency code is required."));
            }

            return;
        }

        if (!IsoCurrencyCodeRegex().IsMatch(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                fieldName,
                "Currency code must be exactly 3 letters (for example LKR)."));
        }
    }

    private static void ValidateBillingStatus(ICollection<ApplicationFieldError> fieldErrors, string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!AllowedBillingStatuses.Contains(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "billingStatus",
                "Billing status must be one of pending, paid, overdue, failed, or waived."));
        }
    }

    private static void ValidateSubscriptionStatus(ICollection<ApplicationFieldError> fieldErrors, string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!AllowedSubscriptionStatuses.Contains(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "subscription.subscriptionStatus",
                "Subscription status is invalid."));
        }
    }

    private static void ValidatePaymentMethod(ICollection<ApplicationFieldError> fieldErrors, string? value)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!AllowedPaymentMethods.Contains(normalized))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "subscription.paymentMethod",
                "Payment method is invalid."));
        }
    }

    private static void ValidateTenantAdmin(
        ICollection<ApplicationFieldError> fieldErrors,
        CreatePlatformTenantAdminRequest tenantAdmin)
    {
        var email = NormalizeOptionalText(tenantAdmin.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            fieldErrors.Add(new ApplicationFieldError("tenantAdmin.email", "Tenant admin email is required."));
            return;
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            fieldErrors.Add(new ApplicationFieldError("tenantAdmin.email", "Tenant admin email is invalid."));
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    [GeneratedRegex("^[A-Za-z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoCountryCodeRegex();

    [GeneratedRegex("^[A-Za-z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoCurrencyCodeRegex();
}


