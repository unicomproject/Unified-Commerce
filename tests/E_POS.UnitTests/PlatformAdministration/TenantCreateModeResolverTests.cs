using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantCreateModeResolverTests
{
    [Theory]
    [InlineData(TenantSubscriptionTypeConstants.Paid, TenantCreateMode.Paid)]
    [InlineData(TenantSubscriptionTypeConstants.Trial, TenantCreateMode.Trial)]
    [InlineData(TenantSubscriptionTypeConstants.Demo, TenantCreateMode.Demo)]
    [InlineData("paid", TenantCreateMode.Paid)]
    [InlineData("trial", TenantCreateMode.Trial)]
    [InlineData("demo", TenantCreateMode.Demo)]
    public void ResolveWizard_ExplicitSubscriptionType_ReturnsExpectedMode(string raw, TenantCreateMode expected)
    {
        var result = TenantCreateModeResolver.ResolveWizard(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Mode);
    }

    [Fact]
    public void ResolveWizard_MissingSubscriptionType_IsNotTrial()
    {
        var result = TenantCreateModeResolver.ResolveWizard(null);

        Assert.False(result.IsSuccess);
        Assert.Equal(TenantCreateModeResolver.ResolutionFailure.MissingSubscriptionType, result.Failure);
    }

    [Fact]
    public void ResolveWizard_UnknownSubscriptionType_IsRejected()
    {
        var result = TenantCreateModeResolver.ResolveWizard("mystery");

        Assert.False(result.IsSuccess);
        Assert.Equal(TenantCreateModeResolver.ResolutionFailure.UnknownSubscriptionType, result.Failure);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("monthly")]
    [InlineData("yearly")]
    [InlineData("demo")]
    [InlineData("trial")]
    [InlineData("TRIAL")]
    public void ResolveWizard_BillingCycleAndSubscriptionStatus_DoNotDetermineMode(
        string? billingCycle)
    {
        var paid = TenantCreateModeResolver.ResolveWizard(TenantSubscriptionTypeConstants.Paid);
        var trial = TenantCreateModeResolver.ResolveWizard(TenantSubscriptionTypeConstants.Trial);

        Assert.Equal(TenantCreateMode.Paid, paid.Mode);
        Assert.Equal(TenantCreateMode.Trial, trial.Mode);

        _ = billingCycle;
    }

    [Fact]
    public void ResolveLegacyMinimalCompatibility_IsIsolatedTrialDefault()
    {
        Assert.Equal(TenantCreateMode.Trial, TenantCreateModeResolver.ResolveLegacyMinimalCompatibility());
    }

    [Theory]
    [InlineData(TenantCreateMode.Paid, TenantStatusConstants.PendingPayment)]
    [InlineData(TenantCreateMode.Trial, TenantStatusConstants.Draft)]
    [InlineData(TenantCreateMode.Demo, TenantStatusConstants.Draft)]
    public void InitialLifecycleStatus_MatchesCreateMode(TenantCreateMode mode, string expected)
    {
        Assert.Equal(expected, TenantCreateModeResolver.InitialLifecycleStatus(mode));
    }
}
