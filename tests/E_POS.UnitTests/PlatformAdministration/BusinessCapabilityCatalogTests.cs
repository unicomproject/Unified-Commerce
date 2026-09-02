using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class BusinessCapabilityCatalogTests
{
    [Fact]
    public void Catalog_Contains_Exactly_19_Business_Modules()
    {
        Assert.Equal(19, BusinessCapabilityCatalog.Modules.Count);
    }

    [Fact]
    public void Catalog_Modules_Have_Unique_Codes_And_Sequential_DisplayOrders()
    {
        var codes = BusinessCapabilityCatalog.Modules.Select(m => m.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        for (int i = 0; i < BusinessCapabilityCatalog.Modules.Count; i++)
        {
            var module = BusinessCapabilityCatalog.Modules[i];
            Assert.Equal(i + 1, module.DisplayOrder);
            Assert.Equal("R1", module.ReleaseCode);
        }
    }

    [Fact]
    public void Catalog_Capabilities_Have_Unique_Codes()
    {
        var allCapCodes = BusinessCapabilityCatalog.Modules
            .SelectMany(m => m.Capabilities)
            .Select(c => c.Code)
            .ToList();

        Assert.Equal(allCapCodes.Count, allCapCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(allCapCodes, string.IsNullOrWhiteSpace);
    }

    [Fact]
    public void All_Mapped_Feature_Codes_Are_Known_In_PlatformTenantFeatureCodes()
    {
        var allFeatureCodes = BusinessCapabilityCatalog.Modules
            .SelectMany(m => m.Capabilities)
            .SelectMany(c => c.MappedTechnicalFeatureCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var featureCode in allFeatureCodes)
        {
            var canonical = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(featureCode);
            Assert.True(PlatformTenantFeatureCodes.IsKnownFeatureCode(canonical), $"Feature {featureCode} should be known in PlatformTenantFeatureCodes");
        }
    }

    [Fact]
    public void BM01_And_BM03_Are_Production_Ready_Closed()
    {
        var bm01 = BusinessCapabilityCatalog.Modules.First(m => m.Code == "BM-01");
        Assert.Equal("PRODUCTION READY / CLOSED", bm01.CurrentR1Status);

        var bm03 = BusinessCapabilityCatalog.Modules.First(m => m.Code == "BM-03");
        Assert.Equal("PRODUCTION READY / CLOSED", bm03.CurrentR1Status);
    }

    [Fact]
    public void R1_Excluded_Capabilities_Are_Classified_As_ExcludedR1()
    {
        foreach (var cap in BusinessCapabilityCatalog.R1ExcludedCapabilities)
        {
            Assert.Equal(CommercialClassification.ExcludedR1, cap.CommercialClassification);
            Assert.Empty(cap.MappedTechnicalFeatureCodes);
        }
    }

    [Fact]
    public async Task Service_Returns_Valid_BusinessCapabilityMap_Response()
    {
        var service = new BusinessCapabilityCatalogService();
        var result = await service.GetBusinessCapabilityMapAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var map = result.Value!;
        Assert.Equal("R1", map.Release);
        Assert.Equal(19, map.Summary.BusinessModuleCount);
        Assert.True(map.Summary.BusinessCapabilityCount > 0);
        Assert.True(map.Summary.TechnicalFeatureCount > 0);
        Assert.True(map.Summary.TenantPermissionCount > 0);

        Assert.Equal(19, map.BusinessModules.Count);
        Assert.Equal("BM-01", map.BusinessModules.First().Code);
        Assert.Equal("BM-19", map.BusinessModules.Last().Code);
    }
}
