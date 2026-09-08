using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class OnlineOrderCanonicalPermissionReconciliationTests
{
    [Fact]
    public void ProductionOnlineOrderCodes_AreInCashierCatalog_AndRoleAssignable()
    {
        foreach (var code in OnlineOrderPickingPermissions.All)
        {
            Assert.True(
                CashierPosPermissionAssignmentRules.TryGetRoleAssignable(code, out _),
                $"Missing role-assignable catalog entry: {code}");
        }

        var effective = CashierPosEffectivePermissionResolver.Resolve(OnlineOrderPickingPermissions.All);
        foreach (var code in OnlineOrderPickingPermissions.All)
        {
            Assert.Contains(code, effective);
        }
    }

    [Fact]
    public void WrongFulfilmentSpelling_IsNotCanonical_AndFilteredByResolver()
    {
        var wrong = OnlineOrderPickingPermissions.FulfilmentStartWrongSpelling;
        Assert.DoesNotContain(wrong, OnlineOrderPickingPermissions.All);
        Assert.False(CashierPosPermissionAssignmentRules.TryGetRoleAssignable(wrong, out _));

        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            OnlineOrderPickingPermissions.FulfilmentStart,
            wrong
        ]);
        Assert.Contains(OnlineOrderPickingPermissions.FulfilmentStart, effective);
        Assert.DoesNotContain(wrong, effective);
    }

    [Fact]
    public void Resolver_KeepsGrantedCanonicalOnlineOrderCodes_RemovesUnknown()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            OnlineOrderPickingPermissions.OrdersAccess,
            OnlineOrderPickingPermissions.OrdersView,
            OnlineOrderPickingPermissions.FulfilmentStart,
            OnlineOrderPickingPermissions.PickingView,
            "commerce.online_order.fulfillment.start",
            "commerce.online_order.orders.invented",
            "pos.online_orders.invented.action"
        ]);

        Assert.Contains(OnlineOrderPickingPermissions.OrdersAccess, effective);
        Assert.Contains(OnlineOrderPickingPermissions.OrdersView, effective);
        Assert.Contains(OnlineOrderPickingPermissions.FulfilmentStart, effective);
        Assert.Contains(OnlineOrderPickingPermissions.PickingView, effective);
        Assert.DoesNotContain("commerce.online_order.fulfillment.start", effective);
        Assert.DoesNotContain("commerce.online_order.orders.invented", effective);
        Assert.DoesNotContain("pos.online_orders.invented.action", effective);
    }

    [Fact]
    public void FeatureAccessAlone_DoesNotImplyWorkflowChildren_InResolver()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            OnlineOrderPickingPermissions.OrdersAccess,
            OnlineOrderPickingPermissions.OrdersView
        ]);

        Assert.Contains(OnlineOrderPickingPermissions.OrdersAccess, effective);
        Assert.Contains(OnlineOrderPickingPermissions.OrdersView, effective);
        Assert.DoesNotContain(OnlineOrderPickingPermissions.FulfilmentStart, effective);
        Assert.DoesNotContain(OnlineOrderPickingPermissions.PickingView, effective);
        Assert.DoesNotContain(OnlineOrderPickingPermissions.PackingPack, effective);
    }

    [Fact]
    public void ReconciliationSeedSql_ContainsCanonicalCodes_AndLegacyMaps()
    {
        var sql = OnlineOrderCanonicalPermissionReconciliationSeedData.UpSql;
        Assert.Contains(OnlineOrderPickingPermissions.OrdersView, sql);
        Assert.Contains(OnlineOrderPickingPermissions.FulfilmentStart, sql);
        Assert.Contains(OnlineOrderPickingPermissions.PickingView, sql);
        Assert.Contains("pos.online_orders.picking.view", sql);
        Assert.DoesNotContain("commerce.online_order.fulfillment.start", sql);
    }
}
