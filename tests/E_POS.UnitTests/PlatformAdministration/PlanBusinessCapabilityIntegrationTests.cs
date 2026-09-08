using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlanBusinessCapabilityIntegrationTests
{
    private static Mock<IPlatformSubscriptionPlanRepository> CreateMockRepository()
    {
        var mockRepo = new Mock<IPlatformSubscriptionPlanRepository>();

        var features = new List<PlanTechnicalFeatureLookupDto>
        {
            new(Guid.Parse("72000000-0000-0000-0000-000000000001"), PlatformTenantFeatureCodes.TenantProfile, "Tenant Profile", "Tenant Profile", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000002"), PlatformTenantFeatureCodes.TenantSettings, "Tenant Settings", "Tenant Settings", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000003"), PlatformTenantFeatureCodes.OutletManagement, "Outlet Management", "Outlet Setup", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000004"), PlatformTenantFeatureCodes.TillManagement, "Till Management", "Till Setup", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000005"), PlatformTenantFeatureCodes.UserAccounts, "User Accounts", "User Accounts", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000006"), PlatformTenantFeatureCodes.RoleManagement, "Role Management", "Role Management", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000007"), PlatformTenantFeatureCodes.PermissionManagement, "Permission Management", "Permissions", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000008"), PlatformTenantFeatureCodes.ProductCatalog, "Product Catalog", "Product Catalog", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000009"), PlatformTenantFeatureCodes.InventoryTracking, "Inventory Tracking", "Stock Management", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000010"), PlatformTenantFeatureCodes.PosCheckout, "POS Checkout", "POS Checkout", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000011"), PlatformTenantFeatureCodes.OnlineStore, "Online Store", "E-commerce Storefront", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000012"), PlatformTenantFeatureCodes.SalesOrders, "Sales Orders", "Sales Orders", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000013"), PlatformTenantFeatureCodes.ClickCollect, "Click & Collect", "Store Pickup", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000014"), PlatformTenantFeatureCodes.SalesReports, "Sales Reports", "Analytics & Reports", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000015"), PlatformTenantFeatureCodes.HardwareDeviceManagement, "Hardware Device Management", "Printers & Cash Drawers", "TENANT", "ACTIVE"),
            new(Guid.Parse("72000000-0000-0000-0000-000000000016"), PlatformTenantFeatureCodes.OfflineOperationSync, "Offline Sync", "Offline Operation", "TENANT", "ACTIVE")
        };

        mockRepo.Setup(r => r.GetActiveTenantFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(features);

        return mockRepo;
    }

    [Fact]
    public async Task GetPlanBusinessModules_ReturnsAll19BusinessModulesWithCapabilities()
    {
        var mockRepo = CreateMockRepository();
        var service = new PlanBusinessCapabilityCatalogService(mockRepo.Object);

        var modules = await service.GetPlanBusinessModulesAsync(null, CancellationToken.None);

        Assert.Equal(19, modules.Count);
        Assert.Equal("BM-01", modules[0].Code);
        Assert.Equal("BM-19", modules[18].Code);

        var totalCaps = modules.Sum(m => m.Capabilities.Count);
        Assert.Equal(41, totalCaps);
    }

    [Fact]
    public async Task GetMandatoryCoreFeatureIds_ReturnsTenantProfileAndTenantSettingsIds()
    {
        var mockRepo = CreateMockRepository();
        var service = new PlanBusinessCapabilityCatalogService(mockRepo.Object);

        var coreIds = await service.GetMandatoryCoreFeatureIdsAsync(CancellationToken.None);

        Assert.Equal(2, coreIds.Count);
        Assert.Contains(Guid.Parse("72000000-0000-0000-0000-000000000001"), coreIds);
        Assert.Contains(Guid.Parse("72000000-0000-0000-0000-000000000002"), coreIds);
    }

    [Fact]
    public async Task ReverseMapping_DerivesCorrectModuleStates()
    {
        var mockRepo = CreateMockRepository();
        var service = new PlanBusinessCapabilityCatalogService(mockRepo.Object);

        // Select OutletManagement (BM-02 feature 1) but NOT TillManagement (BM-02 feature 2)
        var selectedIds = new[] { Guid.Parse("72000000-0000-0000-0000-000000000003") };

        var modules = await service.GetPlanBusinessModulesAsync(selectedIds, CancellationToken.None);

        var bm01 = modules.First(m => m.Code == "BM-01");
        Assert.Equal("CORE", bm01.ModuleSelectionState);

        var bm02 = modules.First(m => m.Code == "BM-02");
        Assert.Equal("PARTIALLY_INCLUDED", bm02.ModuleSelectionState);

        var bm07 = modules.First(m => m.Code == "BM-07");
        Assert.Equal("NOT_INCLUDED", bm07.ModuleSelectionState);
    }

    [Fact]
    public async Task R1ExcludedDefinitions_HaveNoSelectablePlanFeatures()
    {
        var mockRepo = CreateMockRepository();
        var service = new PlanBusinessCapabilityCatalogService(mockRepo.Object);

        var modules = await service.GetPlanBusinessModulesAsync(null, CancellationToken.None);

        var excludedCapabilities = BusinessCapabilityCatalog.R1ExcludedCapabilities;
        Assert.Equal(5, excludedCapabilities.Count);

        foreach (var ex in excludedCapabilities)
        {
            Assert.Equal(CommercialClassification.ExcludedR1, ex.CommercialClassification);
            Assert.Empty(ex.MappedTechnicalFeatureCodes);
        }
    }
}
