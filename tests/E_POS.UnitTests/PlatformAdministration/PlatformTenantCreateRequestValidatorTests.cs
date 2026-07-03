using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Validators;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using E_POS.Domain.Modules.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantCreateRequestValidatorTests
{
    [Fact]
    public void ValidateWizard_CountryCodeLk_Succeeds()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(countryCode: "LK"));

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWizard_CountryCodeSriLanka_FailsBeforeDbSave()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(countryCode: "Sri Lanka"));

        Assert.NotNull(error);
        Assert.Equal("platform_tenants.validation_failed", error!.Code);
        Assert.Contains(error.FieldErrors!, item => item.Field == "countryCode");
        Assert.DoesNotContain("22001", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateWizard_AddressCountryCodeLk_Succeeds()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(
            address: new CreatePlatformTenantAddressRequest { CountryCode = "LK" }));

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWizard_BaseCurrencyLkr_Succeeds()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(baseCurrency: "LKR"));

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWizard_BaseCurrencyLk_FailsValidation()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(baseCurrency: "LK"));

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, item => item.Field == "baseCurrency");
    }

    [Fact]
    public void ValidateWizard_InvalidBillingStatus_FailsValidation()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(billingStatus: "trial"));

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, item => item.Field == "billingStatus");
    }

    [Fact]
    public void ValidateWizard_SubscriptionStatusTrial_IsNotUsedAsBillingStatus()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(
            billingStatus: "pending",
            subscriptionStatus: TenantSubscriptionStatusConstants.Trial));

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWizard_InvalidPayload_ReturnsFieldLevelErrors()
    {
        var error = PlatformTenantCreateRequestValidator.ValidateWizard(CreateRequest(
            countryCode: "Sri Lanka",
            baseCurrency: "LK",
            billingStatus: "trial",
            subscriptionStatus: "trial",
            paymentMethod: "manual",
            tenantAdminEmail: "not-an-email"));

        Assert.NotNull(error);
        Assert.True(error!.FieldErrors!.Count >= 3);
        Assert.All(error.FieldErrors!, item => Assert.False(string.IsNullOrWhiteSpace(item.Field)));
        Assert.All(error.FieldErrors!, item => Assert.False(string.IsNullOrWhiteSpace(item.Message)));
    }

    private static CreatePlatformTenantRequest CreateRequest(
        string? countryCode = "LK",
        string? baseCurrency = "LKR",
        string? billingStatus = TenantBillingStatusConstants.Pending,
        string? subscriptionStatus = TenantSubscriptionStatusConstants.Trial,
        string? paymentMethod = "manual",
        string? tenantAdminEmail = "admin@tenant.com",
        CreatePlatformTenantAddressRequest? address = null)
    {
        return new CreatePlatformTenantRequest
        {
            Code = "TEN-001",
            Name = "Tenant",
            CountryCode = countryCode,
            BaseCurrency = baseCurrency,
            BillingStatus = billingStatus,
            SubscriptionPlanId = Guid.Parse("81111111-1111-4111-8111-111111111111"),
            Address = address,
            TenantAdmin = new CreatePlatformTenantAdminRequest
            {
                FirstName = "Ada",
                Email = tenantAdminEmail,
                SendInvite = true
            },
            Subscription = new CreatePlatformTenantSubscriptionDetailsRequest
            {
                BillingCycle = TenantSubscriptionBillingConstants.BillingCycleMonthly,
                SubscriptionStatus = subscriptionStatus,
                PaymentMethod = paymentMethod
            }
        };
    }
}
