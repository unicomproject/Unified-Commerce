using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class PosPermissionSeedTests
{
    [Fact]
    public void NewSalePermissionDefinitions_ContainSingleCanonicalPosHomeCode()
    {
        var matches = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Count(definition => string.Equals(
                definition.PermissionCode,
                PosPermissions.Home.View,
                StringComparison.Ordinal));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void NewSalePermissionDefinitions_ContainSingleDashboardAliasCode()
    {
        var matches = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Count(definition => string.Equals(
                definition.PermissionCode,
                PosPermissions.Home.ViewDashboard,
                StringComparison.Ordinal));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void NewSalePermissionDefinitions_DoNotContainPosHomeViewDashboardCode()
    {
        var matches = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Count(definition => string.Equals(
                definition.PermissionCode,
                "pos.home.view_dashboard",
                StringComparison.Ordinal));

        Assert.Equal(0, matches);
    }

    [Fact]
    public void PaymentReceiptPermissionDefinitions_ContainSinglePosTillOpenCode()
    {
        var matches = DevelopmentPosPaymentReceiptPermissionsSeedData.Definitions
            .Count(definition => string.Equals(
                definition.PermissionCode,
                PosPermissions.Till.Open,
                StringComparison.Ordinal));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void CashierPermissionAssignments_ContainSinglePosTillOpenCode()
    {
        var matches = DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes
            .Count(code => string.Equals(code, PosPermissions.Till.Open, StringComparison.Ordinal));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void PaymentReceiptPermissionDefinitions_ContainSinglePosTillCloseCode()
    {
        var matches = DevelopmentPosPaymentReceiptPermissionsSeedData.Definitions
            .Count(definition => string.Equals(
                definition.PermissionCode,
                PosPermissions.Till.Close,
                StringComparison.Ordinal));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void CashierPermissionAssignments_ContainSinglePosTillCloseCode()
    {
        var matches = DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes
            .Count(code => string.Equals(code, PosPermissions.Till.Close, StringComparison.Ordinal));

        Assert.Equal(1, matches);
    }

    [Fact]
    public void NewSaleAndPaymentPermissionDefinitions_ContainRequiredCodesExactlyOnce()
    {
        var allCodes = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Select(definition => definition.PermissionCode)
            .Concat(DevelopmentPosPaymentReceiptPermissionsSeedData.Definitions.Select(definition => definition.PermissionCode))
            .ToList();

        var requiredCodes = new[]
        {
            PosPermissions.NewSale.View,
            SalesPermissions.Sale.Create,
            ProductPosPermissions.View,
            ProductPosPermissions.Search,
            SalesPermissions.Cart.Manage,
            SalesPermissions.Cart.AddItem,
            SalesPermissions.Cart.UpdateItem,
            SalesPermissions.Cart.RemoveItem,
            SalesPermissions.Cart.Clear,
            CustomerPermissions.Create,
            SalesPermissions.Discount.Apply,
            SalesPermissions.Park.Create,
            SalesPermissions.Sale.Checkout,
            PaymentPermissions.AcceptCash,
            PaymentPermissions.AcceptCard,
            PaymentPermissions.AcceptQr,
            PaymentPermissions.AcceptSplit,
            PosPermissions.Notifications.View,
            PosPermissions.Till.ViewSession,
            PosPermissions.Till.Close
        };

        foreach (var code in requiredCodes)
        {
            var matches = allCodes.Count(x => string.Equals(x, code, StringComparison.Ordinal));
            Assert.Equal(1, matches);
        }
    }

    [Fact]
    public void CashierPermissionAssignments_ContainRequiredNewSaleCodesWithoutDuplicates()
    {
        var assignments = DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes;
        var distinctCount = assignments.Distinct(StringComparer.Ordinal).Count();
        Assert.Equal(distinctCount, assignments.Count);

        var requiredCodes = new[]
        {
            PosPermissions.NewSale.View,
            SalesPermissions.Sale.Create,
            ProductPosPermissions.View,
            ProductPosPermissions.Search,
            SalesPermissions.Cart.Manage,
            SalesPermissions.Cart.AddItem,
            SalesPermissions.Cart.UpdateItem,
            SalesPermissions.Cart.RemoveItem,
            SalesPermissions.Cart.Clear,
            CustomerPermissions.Create,
            SalesPermissions.Discount.Apply,
            SalesPermissions.Park.Create,
            SalesPermissions.Sale.Checkout,
            PaymentPermissions.AcceptCash,
            PaymentPermissions.AcceptCard,
            PaymentPermissions.AcceptQr,
            PaymentPermissions.AcceptSplit,
            PosPermissions.Notifications.View,
            PosPermissions.Till.ViewSession,
            PosPermissions.Till.Close
        };

        foreach (var code in requiredCodes)
        {
            Assert.Contains(code, assignments);
        }
    }

    [Fact]
    public void DiscountApprovalPermission_IsSeededButNotGrantedToCashierByDefault()
    {
        Assert.Contains(
            DevelopmentPosDiscountWorkflowSeedData.PermissionDefinitions,
            x => x.PermissionCode == SalesPermissions.Discount.Approve);
        Assert.DoesNotContain(
            SalesPermissions.Discount.Approve,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes);
    }
}
