using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantSubscriptionLimitCatalogTests
{
    [Fact]
    public void CanonicalKeys_ContainEnforcedCapacityLimits()
    {
        Assert.True(TenantSubscriptionLimitKeys.TryGet(TenantSubscriptionLimitKeys.MaxOutlets, out var outlets));
        Assert.Equal(RuntimeEnforcementStatus.Enforced, outlets.Status);
        Assert.True(TenantSubscriptionLimitKeys.TryGet(TenantSubscriptionLimitKeys.MaxTills, out var tills));
        Assert.Equal(RuntimeEnforcementStatus.Enforced, tills.Status);
        Assert.True(TenantSubscriptionLimitKeys.TryGet(TenantSubscriptionLimitKeys.MaxUsers, out var users));
        Assert.Equal(RuntimeEnforcementStatus.Enforced, users.Status);
    }

    [Fact]
    public void ProductAndDeviceLimits_AreBlockedPendingCanonicalDefinition()
    {
        Assert.True(TenantSubscriptionLimitKeys.TryGet(TenantSubscriptionLimitKeys.MaxProducts, out var products));
        Assert.Equal(RuntimeEnforcementStatus.BlockedPendingCanonicalDefinition, products.Status);
        Assert.True(TenantSubscriptionLimitKeys.TryGet(TenantSubscriptionLimitKeys.MaxDevices, out var devices));
        Assert.Equal(RuntimeEnforcementStatus.BlockedPendingCanonicalDefinition, devices.Status);
    }

    [Fact]
    public void LimitReachedError_IncludesCapacityFields()
    {
        var evaluation = new TenantResourceLimitEvaluation(
            TenantSubscriptionLimitKeys.MaxOutlets,
            TenantSubscriptionLimitKeys.ResourceOutlets,
            3,
            1,
            3,
            0,
            false,
            false,
            true,
            SubscriptionLimitErrorCodes.LimitReached,
            "Outlets subscription limit reached.");

        var error = evaluation.ToApplicationError();
        Assert.NotNull(error);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, error!.Code);
        Assert.Contains(error.FieldErrors!, x => x.Field == "currentUsage" && x.Message == "3");
        Assert.Contains(error.FieldErrors!, x => x.Field == "effectiveLimit" && x.Message == "3");
        Assert.Contains(error.FieldErrors!, x => x.Field == "resource" && x.Message == "outlets");
    }
}
