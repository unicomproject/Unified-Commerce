using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantFeatureCodesTests
{
    [Fact]
    public void OutletManagement_IsCanonicalKey()
    {
        Assert.Equal("outlet_management", PlatformTenantFeatureCodes.OutletManagement);
        Assert.Equal("outlet_management", OutletConstantsProxy.ManagementFeatureCode);
    }

    [Fact]
    public void LegacyOutletAlias_MapsToCanonical()
    {
        Assert.True(PlatformTenantFeatureCodes.TryGetCanonicalFeatureCode(
            PlatformTenantFeatureCodes.OutletManagementLegacyAlias,
            out var canonical));
        Assert.Equal(PlatformTenantFeatureCodes.OutletManagement, canonical);
        Assert.True(PlatformTenantFeatureCodes.IsLegacyAlias(PlatformTenantFeatureCodes.OutletManagementLegacyAlias));
    }

    [Fact]
    public void LookupOrder_PutsCanonicalBeforeLegacyAlias()
    {
        var codes = PlatformTenantFeatureCodes.GetLookupFeatureCodes(
            PlatformTenantFeatureCodes.OutletManagementLegacyAlias);

        Assert.Equal(
            [
                PlatformTenantFeatureCodes.OutletManagement,
                PlatformTenantFeatureCodes.OutletManagementLegacyAlias
            ],
            codes);
    }

    [Fact]
    public void UnknownAlias_IsNotKnown()
    {
        Assert.False(PlatformTenantFeatureCodes.IsKnownFeatureCode("tenant_admin.unknown"));
        Assert.False(PlatformTenantFeatureCodes.TryGetCanonicalFeatureCode("tenant_admin.unknown", out _));
    }

    [Fact]
    public void IsOutletManagementFeatureCode_RecognizesCanonicalAndLegacyOnly()
    {
        Assert.True(PlatformTenantFeatureCodes.IsOutletManagementFeatureCode("outlet_management"));
        Assert.True(PlatformTenantFeatureCodes.IsOutletManagementFeatureCode("tenant_admin.outlets"));
        Assert.False(PlatformTenantFeatureCodes.IsOutletManagementFeatureCode("tenant.outlet"));
        Assert.False(PlatformTenantFeatureCodes.IsOutletManagementFeatureCode("till_management"));
    }

    private static class OutletConstantsProxy
    {
        public static string ManagementFeatureCode =>
            E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants.OutletConstants.ManagementFeatureCode;
    }
}
