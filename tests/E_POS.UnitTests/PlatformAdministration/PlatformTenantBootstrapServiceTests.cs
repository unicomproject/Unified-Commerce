using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantBootstrapServiceTests
{
    [Fact]
    public void SetupHubEvaluator_WhenNoOutlets_ReturnsNotStartedForOutlets()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 0,
                ActiveTillCount: 0,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: false));

        var outlets = modules.Single(module => module.ModuleKey == "outlets");
        var tills = modules.Single(module => module.ModuleKey == "tills");
        var users = modules.Single(module => module.ModuleKey == "users");

        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotStarted, outlets.Status);
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusBlocked, tills.Status);
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotStarted, users.Status);
        Assert.Equal("Create an outlet before adding tills.", tills.DependencyNotice);
    }

    [Fact]
    public void SetupHubEvaluator_WhenOutletConfigured_ReturnsConfiguredStates()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 1,
                TenantUserCount: 2,
                ActiveOrDraftProductCount: 3,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: true,
                OnlineStoreStatus: "ACTIVE",
                CanManageOnlineStore: true));

        Assert.All(modules, module => Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusConfigured, module.Status));
    }

    [Fact]
    public void SetupHubEvaluator_WhenRolesNotCreated_ReturnsNotRequired()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: false));

        var roles = modules.Single(module => module.ModuleKey == "roles");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotRequired, roles.Status);
    }

    [Fact]
    public void SetupHubEvaluator_WhenProductsNotEntitled_ReturnsNotEntitled()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: false,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: false));

        var products = modules.Single(module => module.ModuleKey == "products");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotEntitled, products.Status);
        Assert.False(products.Entitled);
    }

    [Fact]
    public void SetupHubEvaluator_WhenOutletsNotEntitled_ReturnsNotEntitled()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: false,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 0,
                ActiveTillCount: 0,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: false));

        var outlets = modules.Single(module => module.ModuleKey == "outlets");
        var tills = modules.Single(module => module.ModuleKey == "tills");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotEntitled, outlets.Status);
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusBlocked, tills.Status);
    }

    [Fact]
    public void SetupHubEvaluator_WhenOnlyDefaultUser_ReturnsNotStartedForUsers()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: false));

        var users = modules.Single(module => module.ModuleKey == "users");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotStarted, users.Status);
    }

    [Fact]
    public void SetupHubEvaluator_WhenUsersConfigured_ReturnsConfigured()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 2,
                ActiveOrDraftProductCount: 1,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: false));

        var users = modules.Single(module => module.ModuleKey == "users");
        var products = modules.Single(module => module.ModuleKey == "products");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusConfigured, users.Status);
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusConfigured, products.Status);
    }

    [Fact]
    public void SetupHubEvaluator_WhenOnlineStoreNotEntitled_ReturnsNotEntitled()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: false,
                OnlineStoreStatus: null,
                CanManageOnlineStore: true));

        var onlineStore = modules.Single(module => module.ModuleKey == "online_store");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotEntitled, onlineStore.Status);
        Assert.False(onlineStore.Entitled);
        Assert.False(onlineStore.CanConfigure);
    }

    [Fact]
    public void SetupHubEvaluator_WhenOnlineStoreDraft_ReturnsNotStarted()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: true,
                OnlineStoreStatus: "DRAFT",
                CanManageOnlineStore: true));

        var onlineStore = modules.Single(module => module.ModuleKey == "online_store");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusNotStarted, onlineStore.Status);
        Assert.True(onlineStore.Entitled);
        Assert.True(onlineStore.CanConfigure);
        Assert.Equal(0, onlineStore.Count);
    }

    [Fact]
    public void SetupHubEvaluator_WhenOnlineStoreActive_ReturnsConfigured()
    {
        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                OutletEntitled: true,
                TillEntitled: true,
                ProductEntitled: true,
                ActiveOutletCount: 1,
                ActiveTillCount: 1,
                CustomRoleCount: 0,
                TenantUserCount: 1,
                ActiveOrDraftProductCount: 0,
                TenantSuspended: false,
                CanManageOutlets: true,
                CanManageTills: true,
                CanManageRoles: true,
                CanManageUsers: true,
                CanManageProducts: true,
                OnlineStoreEntitled: true,
                OnlineStoreStatus: "ACTIVE",
                CanManageOnlineStore: true));

        var onlineStore = modules.Single(module => module.ModuleKey == "online_store");
        Assert.Equal(PlatformSelectedTenantSetupHubStatusEvaluator.StatusConfigured, onlineStore.Status);
        Assert.True(onlineStore.Entitled);
        Assert.True(onlineStore.CanConfigure);
        Assert.Equal(1, onlineStore.Count);
    }

    [Fact]
    public void ProductImportParser_BuildsCanonicalTemplateHeader()
    {
        var template = PlatformTenantBootstrapProductImportParser.BuildTemplateCsv();

        Assert.Contains("oneverz_bootstrap_product_import_version=1", template, StringComparison.Ordinal);
        Assert.Contains("product_name,sku,selling_price", template, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductImportParser_RejectsDuplicateSkuInFile()
    {
        var csv = """
            product_name,sku,selling_price,category_code,brand_code,barcode,track_inventory,outlet_code,opening_stock,status
            Rice 1kg,RICE-1,10.00,,,,,,,
            Rice 2kg,RICE-1,12.00,,,,,,,
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var parsed = PlatformTenantBootstrapProductImportParser.Parse(stream, "products.csv");

        Assert.True(parsed.IsSuccess);
        Assert.Equal(2, parsed.Rows.Count);
    }
}
