using E_POS.Application.Modules.Tenant.AccessControl.Mappers;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class TenantPermissionPresentationModuleCatalogTests
{
    [Theory]
    [InlineData("tenant.dashboard.view", "dashboard")]
    [InlineData("pos.dashboard.view", "dashboard")]
    [InlineData("tenant.outlets.create", "outlets")]
    [InlineData("tenant.tills.manage", "tills")]
    [InlineData("till.session.view", "tills")]
    [InlineData("tenant.hardware.manage", "tills")]
    [InlineData("tenant.users.invites.resend", "users")]
    [InlineData("tenant.roles.permissions.update", "roles-access")]
    [InlineData("catalog.products.view", "products")]
    [InlineData("products.view", "products")]
    [InlineData("pricing.price_lists.manage", "products")]
    [InlineData("tenant.stock.transfers.create", "inventory")]
    [InlineData("sales.checkout", "sales_pos")]
    [InlineData("payments.cash.accept", "sales_pos")]
    [InlineData("workspace.pos.access", "sales_pos")]
    [InlineData("tenant.reports.products.view", "reports")]
    [InlineData("ecommerce.storefront.manage", "online-store")]
    [InlineData("commerce.online_order.orders.view", "online-store")]
    [InlineData("workspace.tenant_admin.access", "settings")]
    public void Resolve_MapsPermissionNamespaceToFeatureModule(
        string permissionCode,
        string expectedModuleCode)
    {
        var module = TenantPermissionPresentationModuleCatalog.Resolve(permissionCode);

        Assert.Equal(expectedModuleCode, module.Code);
    }

    [Fact]
    public void Resolve_ReturnsStablePresentationMetadata()
    {
        var first = TenantPermissionPresentationModuleCatalog.Resolve("tenant.outlets.view");
        var second = TenantPermissionPresentationModuleCatalog.Resolve("tenant.outlets.update");

        Assert.Equal(first, second);
        Assert.Equal("Outlets", first.Name);
        Assert.Equal(2, first.SortOrder);
        Assert.NotEqual(Guid.Empty, first.Id);
    }
}
