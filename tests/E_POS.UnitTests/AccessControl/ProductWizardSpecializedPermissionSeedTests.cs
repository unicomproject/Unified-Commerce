using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class ProductWizardSpecializedPermissionSeedTests
{
    [Fact]
    public void UpSql_DefinesWizardStartAndPublishPermissions()
    {
        var sql = ProductWizardSpecializedPermissionSeedData.UpSql;

        Assert.Contains(ProductConstants.BarcodesManagePermission, sql);
        Assert.Contains(ProductConstants.ProductPricingManagePermission, sql);
        Assert.Contains(ProductConstants.PublishPermission, sql);
        Assert.Contains(ProductConstants.VariantsManagePermission, sql);
        Assert.Contains(ProductConstants.ComboComponentsManagePermission, sql);
        Assert.Contains(ProductConstants.TaxClassesViewPermission, sql);
        Assert.Contains("TENANT_ADMIN", sql);
        Assert.Contains("catalog.products.create", sql);
        Assert.DoesNotContain("catalog.product_media.manage", sql);
        Assert.DoesNotContain("inventory.stock.adjust", sql);
    }
}
