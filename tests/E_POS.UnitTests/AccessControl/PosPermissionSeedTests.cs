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
            SalesPermissions.Discount.Apply,
            SalesPermissions.Park.Create,
            SalesPermissions.Park.View,
            SalesPermissions.Park.Recall,
            SalesPermissions.Sale.Checkout,
            PaymentPermissions.AcceptCash,
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
            SalesPermissions.Discount.Apply,
            SalesPermissions.Park.Create,
            SalesPermissions.Park.View,
            SalesPermissions.Park.Recall,
            SalesPermissions.Sale.Checkout,
            PaymentPermissions.AcceptCash,
            PosPermissions.Notifications.View,
            PosPermissions.Till.ViewSession,
            PosPermissions.Till.Close
        };

        foreach (var code in requiredCodes)
        {
            Assert.Contains(code, assignments);
        }

        Assert.DoesNotContain(PaymentPermissions.AcceptCard, assignments);
        Assert.DoesNotContain(PaymentPermissions.AcceptQr, assignments);
        Assert.DoesNotContain(PaymentPermissions.AcceptSplit, assignments);
    }

    [Fact]
    public void ReturnsExchangePermissions_AreSeededWithExpectedCodes()
    {
        var codes = DevelopmentPosReturnsExchangePermissionsSeedData.Definitions
            .Select(definition => definition.PermissionCode)
            .ToList();

        Assert.Contains(ReturnsPermissions.CreateReturn, codes);
        Assert.Contains(ReturnsPermissions.ViewExchanges, codes);
        Assert.Contains(ReturnsPermissions.CreateExchange, codes);
        Assert.Contains(ReturnsPermissions.ApproveRefund, codes);
    }

    [Fact]
    public void RefundApprovalPermission_IsSeededButNotGrantedToCashierByDefault()
    {
        Assert.Contains(
            DevelopmentPosReturnsExchangePermissionsSeedData.Definitions,
            x => x.PermissionCode == ReturnsPermissions.ApproveRefund);
        Assert.DoesNotContain(
            ReturnsPermissions.ApproveRefund,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes);
        Assert.Contains(
            ReturnsPermissions.CreateReturn,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes);
        Assert.Contains(
            ReturnsPermissions.CreateExchange,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes);
        Assert.Contains(
            ReturnsPermissions.ApproveRefund,
            DevelopmentPosReturnsExchangePermissionsSeedData.ManagerPermissionCodes);
    }

    [Fact]
    public void CanonicalReturnBranchPermissions_AreSeededExactlyOnce()
    {
        var paymentCodes = DevelopmentPosPaymentReceiptPermissionsSeedData.Definitions
            .Select(definition => definition.PermissionCode)
            .ToList();
        var returnsExchangeCodes = DevelopmentPosReturnsExchangePermissionsSeedData.Definitions
            .Select(definition => definition.PermissionCode)
            .ToList();
        var allCodes = paymentCodes.Concat(returnsExchangeCodes).ToList();

        Assert.Equal(1, allCodes.Count(code => code == ReturnsPermissions.ViewReturns));
        Assert.Equal(1, allCodes.Count(code => code == ReturnsPermissions.CreateRefund));
        Assert.Equal(1, allCodes.Count(code => code == ReturnsPermissions.CreateReturn));
        Assert.Equal(1, allCodes.Count(code => code == ReturnsPermissions.CreateExchange));
    }

    [Fact]
    public void CustomerUpdatePermission_IsSeededExactlyOnceWithUniqueId()
    {
        var updateDefinitions = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Where(definition => string.Equals(
                definition.PermissionCode,
                CustomerPermissions.Update,
                StringComparison.Ordinal))
            .ToList();

        Assert.Single(updateDefinitions);
        Assert.Equal(DevelopmentPosCustomerUpdatePermissionSeedData.PermissionId, updateDefinitions[0].Id);
        Assert.Equal(
            DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId,
            updateDefinitions[0].FeatureId);
        Assert.Equal("update", updateDefinitions[0].ActionType);
        Assert.Equal(
            DevelopmentPosCustomerUpdatePermissionSeedData.PermissionId,
            DevelopmentPosCustomerUpdatePermissionSeedData.Definition.Id);
    }

    [Fact]
    public void CustomerCreatePermission_IsActiveCanonicalNewSaleDefinition()
    {
        var createDefinitions = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Where(definition => string.Equals(
                definition.PermissionCode,
                CustomerPermissions.Create,
                StringComparison.Ordinal))
            .ToList();

        Assert.Single(createDefinitions);
        Assert.Equal(
            DevelopmentPosCustomerCreatePermissionSeedData.PermissionId,
            createDefinitions[0].Id);
        Assert.Equal(
            DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId,
            createDefinitions[0].FeatureId);
        Assert.Equal("create", createDefinitions[0].ActionType);
    }

    [Fact]
    public void CashierPermissionAssignments_ContainCustomersViewAndCreateWithoutOverGrant()
    {
        var assignments = DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes;

        Assert.Contains(CustomerPermissions.View, assignments);
        Assert.Contains(CustomerPermissions.Create, assignments);
        Assert.Equal(1, assignments.Count(code => code == CustomerPermissions.Create));
        Assert.DoesNotContain("customers.delete", assignments);
        Assert.DoesNotContain("tenant.users.manage", assignments);
        Assert.DoesNotContain("tenant.roles.manage", assignments);
    }

    [Fact]
    public void RestorePosCustomerCreatePermissionSql_IsScopedAndIdempotencyAware()
    {
        var upSql = DevelopmentPosCustomerCreatePermissionSeedData.UpSql
                    + Environment.NewLine
                    + DevelopmentPosCustomerCreatePermissionSeedData.CashierAssignmentUpSql;

        Assert.Contains("customers.create", upSql, StringComparison.Ordinal);
        Assert.Contains(
            DevelopmentPosCustomerCreatePermissionSeedData.PermissionId.ToString(),
            upSql,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE NOT EXISTS", upSql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT DO NOTHING", upSql, StringComparison.Ordinal);
        Assert.DoesNotContain("customers.update", upSql, StringComparison.Ordinal);
        Assert.DoesNotContain("customers.delete", upSql, StringComparison.Ordinal);
    }

    [Fact]
    public void PosPermissionSeedIds_AreUniqueAcrossCatalogues()
    {
        var allIds = DevelopmentPosNewSalePermissionsSeedData.Definitions
            .Select(definition => definition.Id)
            .Concat(DevelopmentPosPaymentReceiptPermissionsSeedData.Definitions.Select(definition => definition.Id))
            .Concat(DevelopmentPosReturnsExchangePermissionsSeedData.Definitions.Select(definition => definition.Id))
            .Concat(DevelopmentPosOnlineOrderPermissionsSeedData.Definitions.Select(definition => definition.Id))
            .Concat(DevelopmentPosDiscountWorkflowSeedData.PermissionDefinitions.Select(definition => definition.Id))
            .ToList();

        Assert.Equal(allIds.Count, allIds.Distinct().Count());
        Assert.DoesNotContain(
            Guid.Parse("77777777-0316-4000-8000-000000000001"),
            DevelopmentPosNewSalePermissionsSeedData.Definitions.Select(definition => definition.Id));
        Assert.Contains(
            Guid.Parse("77777777-0316-4000-8000-000000000001"),
            DevelopmentPosPaymentReceiptPermissionsSeedData.Definitions
                .Where(definition => definition.PermissionCode == SalesPermissions.Sale.Checkout)
                .Select(definition => definition.Id));
    }

    [Fact]
    public void CashierPermissionAssignments_ContainCustomersUpdate()
    {
        Assert.Contains(
            CustomerPermissions.Update,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes);
        Assert.Equal(
            1,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes
                .Count(code => code == CustomerPermissions.Update));
    }

    [Fact]
    public void SeedPosCustomerUpdatePermission_UpSql_TargetsOnlyCustomersUpdate()
    {
        var upSql = DevelopmentPosCustomerUpdatePermissionSeedData.UpSql
                    + Environment.NewLine
                    + DevelopmentPosCustomerUpdatePermissionSeedData.CashierAssignmentUpSql;

        Assert.Contains("customers.update", upSql, StringComparison.Ordinal);
        Assert.Contains(
            DevelopmentPosCustomerUpdatePermissionSeedData.PermissionId.ToString(),
            upSql,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.home.view", upSql, StringComparison.Ordinal);
        Assert.DoesNotContain("sales.checkout", upSql, StringComparison.Ordinal);
        Assert.DoesNotContain("ON CONFLICT (permission_code)", upSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedPosCustomerUpdatePermission_DownSql_RemovesOnlyMigrationOwnedRow()
    {
        var downSql = DevelopmentPosCustomerUpdatePermissionSeedData.DownSql;

        Assert.Contains(
            DevelopmentPosCustomerUpdatePermissionSeedData.PermissionId.ToString(),
            downSql,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AND permission_code = 'customers.update'", downSql, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "WHERE permission_code = 'customers.update';",
            downSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HardwareSettingsPermission_IsSeededAndGrantedToCashier()
    {
        Assert.Equal(
            "pos.hardware.settings",
            DevelopmentPosHardwareSettingsPermissionSeedData.PermissionCode);
        Assert.Contains(
            DevelopmentPosHardwareSettingsPermissionSeedData.PermissionCode,
            DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes);
        Assert.Contains(
            "pos.hardware.settings",
            DevelopmentPosHardwareSettingsPermissionSeedData.UpSql,
            StringComparison.Ordinal);
        Assert.Contains(
            DevelopmentTenantSeedConstants.CashierRoleId.ToString(),
            DevelopmentPosHardwareSettingsPermissionSeedData.CashierAssignmentUpSql,
            StringComparison.OrdinalIgnoreCase);
    }
}
