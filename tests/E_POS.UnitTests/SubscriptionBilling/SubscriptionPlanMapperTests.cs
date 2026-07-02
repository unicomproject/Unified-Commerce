using E_POS.Application.Modules.SubscriptionBilling.Mappers;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;
using Xunit;

namespace E_POS.UnitTests.SubscriptionBilling;

public sealed class SubscriptionPlanMapperTests
{
    [Theory]
    [InlineData("monthly", SubscriptionPlanConstants.BillingInterval.Monthly)]
    [InlineData("yearly", SubscriptionPlanConstants.BillingInterval.Yearly)]
    [InlineData("one_time", SubscriptionPlanConstants.BillingInterval.OneTime)]
    public void ToDbBillingInterval_MapsApiValues(string billingCycle, string expected)
    {
        Assert.Equal(expected, SubscriptionPlanMapper.ToDbBillingInterval(billingCycle));
    }

    [Theory]
    [InlineData("published", SubscriptionPlanConstants.Status.Active)]
    [InlineData("archived", SubscriptionPlanConstants.Status.Retired)]
    [InlineData("draft", SubscriptionPlanConstants.Status.Draft)]
    public void NormalizeApiStatus_MapsLegacyFilters(string status, string expected)
    {
        Assert.Equal(expected, SubscriptionPlanMapper.NormalizeApiStatus(status));
    }
}
