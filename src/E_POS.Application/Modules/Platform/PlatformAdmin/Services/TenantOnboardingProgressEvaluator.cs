using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public static class TenantOnboardingProgressEvaluator
{
    public sealed record Result(short Mask, short Percent, IReadOnlyList<int> Steps, IReadOnlyList<ApplicationFieldErrorDto> Errors);

    public static Result Evaluate(TenantOnboardingPayloadDto payload)
    {
        var mask = 0;
        var errors = new List<ApplicationFieldErrorDto>();
        var basic = payload.BasicDetails;
        if (basic is not null && Required(basic.DisplayName, basic.LegalName, basic.TenantCode, basic.TenantSlug,
                basic.BusinessTypeCode, basic.OperatingMode, basic.DefaultCountryCode, basic.BaseCurrencyCode, basic.Timezone, basic.Locale)) mask |= 1;
        else errors.Add(new("basicDetails", "Complete all required tenant basic details."));

        var business = payload.BusinessContact;
        if (business?.RegisteredAddress is { } a && !string.IsNullOrWhiteSpace(a.Line1) && !string.IsNullOrWhiteSpace(a.CountryCode)
            && business.PrimaryContact is { } p && Required(p.Name, p.Email, p.Phone)) mask |= 2;
        else errors.Add(new("businessContact", "Registered address and primary contact are required."));

        var plan = payload.Plan;
        if (plan?.SubscriptionPlanId is not null && plan.SubscriptionPlanId != Guid.Empty && Required(plan.SubscriptionType, plan.BillingCycle)) mask |= 4;
        else errors.Add(new("plan", "Select an active plan, subscription type, and billing cycle."));

        if (payload.Billing is not null && (!string.Equals(plan?.SubscriptionType, "PAID", StringComparison.OrdinalIgnoreCase)
            || Required(payload.Billing.InvoiceEmail, payload.Billing.PaymentMethod))) mask |= 8;
        else errors.Add(new("billing", "Paid onboarding requires invoice email and payment method."));

        if (payload.Entitlements is not null) mask |= 16;
        else errors.Add(new("entitlements", "Feature entitlement selection must be reviewed."));

        var admin = payload.TenantAdmin;
        if (admin is not null && Required(admin.FirstName, admin.Email)) mask |= 32;
        else errors.Add(new("tenantAdmin", "Tenant Admin first name and email are required."));

        if (payload.ReviewConfirmed && mask == 63) mask |= 64;
        else errors.Add(new("reviewConfirmed", "Review confirmation is required."));

        var steps = Enumerable.Range(1, 7).Where(step => (mask & (1 << (step - 1))) != 0).ToArray();
        var percent = (short)Math.Floor(100m * steps.Length / 7m);
        return new((short)mask, percent, steps, errors);
    }

    private static bool Required(params string?[] values) => values.All(value => !string.IsNullOrWhiteSpace(value));
}
