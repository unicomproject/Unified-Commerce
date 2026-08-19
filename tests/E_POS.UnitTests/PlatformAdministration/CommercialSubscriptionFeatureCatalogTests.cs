using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class CommercialSubscriptionFeatureCatalogTests
{
    [Theory]
    [InlineData("pos_checkout")]
    [InlineData("product_catalog")]
    [InlineData("outlet_management")]
    [InlineData("offline_operation_sync")]
    public void IsCommercialSubscriptionSelectable_AcceptsCanonicalKeys(string featureCode)
    {
        Assert.True(CommercialSubscriptionFeatureCatalog.IsCommercialSubscriptionSelectable(featureCode));
    }

    [Theory]
    [InlineData("pos.sales")]
    [InlineData("product_brands")]
    [InlineData("tenant.till_ops")]
    public void IsCommercialSubscriptionSelectable_RejectsTechnicalKeys(string featureCode)
    {
        Assert.False(CommercialSubscriptionFeatureCatalog.IsCommercialSubscriptionSelectable(featureCode));
    }

    [Fact]
    public void NormalizeEntitlementFeatureCodes_MapsTechnicalPosToPosCheckout()
    {
        var normalized = CommercialSubscriptionFeatureCatalog.NormalizeEntitlementFeatureCodes(
        [
            "pos.sales",
            "pos.till",
            "pos_checkout"
        ]);

        Assert.Equal(["pos_checkout"], normalized);
    }

    [Fact]
    public void NormalizeEntitlementFeatureCodes_MapsProductTechnicalIdentifiersToProductCatalog()
    {
        var normalized = CommercialSubscriptionFeatureCatalog.NormalizeEntitlementFeatureCodes(
        [
            "product_barcodes",
            "product_variants",
            "product_catalog"
        ]);

        Assert.Equal(["product_catalog"], normalized);
    }

    [Fact]
    public void TryNormalizeToCommercialEntitlement_RejectsTenantTillOps()
    {
        Assert.False(CommercialSubscriptionFeatureCatalog.TryNormalizeToCommercialEntitlement(
            "tenant.till_ops",
            out _));
    }
}
