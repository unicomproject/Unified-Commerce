using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class CashierPosChunk5EffectivePermissionResolverTests
{
    private const string ExactCash = "pos.cash_payment.tender.exact";
    private const string Numpad = "pos.cash_payment.numpad.container";
    private const string CustomerView = "pos.customers.management.view";
    private const string CustomerPhone = "pos.customers.list.phone";
    private const string CustomerEmail = "pos.customers.list.email";

    [Fact]
    public void RoleParentAndChild_BothEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            ExactCash,
        ]);

        Assert.Contains(PaymentPermissions.AcceptCash, effective);
        Assert.Contains(ExactCash, effective);
    }

    [Fact]
    public void RoleParentOnly_ChildNotEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
        ]);

        Assert.Contains(PaymentPermissions.AcceptCash, effective);
        Assert.DoesNotContain(ExactCash, effective);
    }

    [Fact]
    public void OrphanChild_NotEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve([ExactCash]);

        Assert.DoesNotContain(ExactCash, effective);
        Assert.Empty(effective);
    }

    [Fact]
    public void RoleParent_PlusUserChild_BothEffective()
    {
        // Simulates role grant parent + user override child unioned into candidate set.
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            ExactCash,
        ]);

        Assert.Contains(PaymentPermissions.AcceptCash, effective);
        Assert.Contains(ExactCash, effective);
    }

    [Fact]
    public void ParentFromOneRole_ChildFromAnother_BothEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            ExactCash,
        ]);

        Assert.Contains(ExactCash, effective);
    }

    [Fact]
    public void UserGrantOnly_RootPermission_IsEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCard,
        ]);

        Assert.Equal([PaymentPermissions.AcceptCard], effective);
    }

    [Fact]
    public void ParentRevoked_ChildStored_ChildNotEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve([ExactCash]);
        Assert.DoesNotContain(ExactCash, effective);
    }

    [Fact]
    public void ChildRevoked_ParentRemainsEffective()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
        ]);

        Assert.Contains(PaymentPermissions.AcceptCash, effective);
        Assert.DoesNotContain(ExactCash, effective);
    }

    [Fact]
    public void SiblingIndependence()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            Numpad,
        ]);

        Assert.Contains(PaymentPermissions.AcceptCash, effective);
        Assert.Contains(Numpad, effective);
        Assert.DoesNotContain(ExactCash, effective);
    }

    [Theory]
    [InlineData("pre_auth.login.screen.view")]
    [InlineData("pre_auth.login.branding.view")]
    [InlineData("pre_auth.login.email.input")]
    [InlineData("pre_auth.login.password.input")]
    [InlineData("pre_auth.login.password_visibility.toggle")]
    [InlineData("pre_auth.login.submit.execute")]
    [InlineData("pre_auth.login.validation.message")]
    public void PreAuth_NeverEffective(string code)
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            code,
        ]);

        Assert.DoesNotContain(code, effective);
        Assert.Contains(PaymentPermissions.AcceptCash, effective);
    }

    [Theory]
    [InlineData("pos.cash_drawer.*")]
    [InlineData("pos.till.session.open/close")]
    [InlineData("pos.sales.fake.action")]
    [InlineData("pos.payments.cash")]
    public void InvalidOrUnknown_ExcludedSafely(string code)
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            code,
        ]);

        Assert.DoesNotContain(code, effective);
        Assert.Contains(PaymentPermissions.AcceptCash, effective);
    }

    [Fact]
    public void MultipleRoles_UnionDedupes()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            PaymentPermissions.AcceptCash,
            PaymentPermissions.AcceptCard,
            ExactCash,
        ]);

        Assert.Equal(3, effective.Count);
        Assert.Equal(
            effective.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            effective.Count);
    }

    [Theory]
    [InlineData(PaymentPermissions.AcceptCash)]
    [InlineData(PaymentPermissions.AcceptCard)]
    [InlineData(PaymentPermissions.AcceptQr)]
    [InlineData(PaymentPermissions.AcceptSplit)]
    public void PaymentMethods_Independent(string method)
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve([method]);

        Assert.Equal([method], effective);
        foreach (var other in new[]
                 {
                     PaymentPermissions.AcceptCash,
                     PaymentPermissions.AcceptCard,
                     PaymentPermissions.AcceptQr,
                     PaymentPermissions.AcceptSplit,
                 })
        {
            if (string.Equals(other, method, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Assert.DoesNotContain(other, effective);
        }
    }

    [Fact]
    public void CashInternal_ExactYes_NumpadNo()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
            ExactCash,
        ]);

        Assert.Contains(ExactCash, effective);
        Assert.DoesNotContain(Numpad, effective);
    }

    [Fact]
    public void SensitiveChild_PhoneYes_EmailNo()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            CustomerView,
            CustomerPhone,
        ]);

        Assert.Contains(CustomerPhone, effective);
        Assert.DoesNotContain(CustomerEmail, effective);
    }

    [Fact]
    public void NonCashierNamespace_PassesThroughWithoutParentGraph()
    {
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            "tenant.users.management.manage",
            "tenant.roles.permissions.update",
        ]);

        Assert.Contains("tenant.users.management.manage", effective);
        Assert.Contains("tenant.roles.permissions.update", effective);
    }

    [Fact]
    public void Output_IsSorted_Deterministic()
    {
        var a = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptSplit,
            PaymentPermissions.AcceptCash,
            PaymentPermissions.AcceptCard,
        ]);
        var b = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCard,
            PaymentPermissions.AcceptSplit,
            PaymentPermissions.AcceptCash,
        ]);

        Assert.Equal(a, b);
        Assert.Equal(
            a.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            a.ToArray());
    }

    [Fact]
    public void CatalogSize_IsNotAutoGranted()
    {
        Assert.Equal(346, CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes.Count);
        var effective = CashierPosEffectivePermissionResolver.Resolve(
        [
            PaymentPermissions.AcceptCash,
        ]);
        Assert.Single(effective);
    }

    [Fact]
    public void Resolution_DoesNotThrow_OnEmptyOrNull()
    {
        Assert.Empty(CashierPosEffectivePermissionResolver.Resolve(null));
        Assert.Empty(CashierPosEffectivePermissionResolver.Resolve(Array.Empty<string>()));
    }

    [Fact]
    public void CommerceParent_CanSatisfyPosChild()
    {
        const string homeOnlineOrders = "pos.home.actions.online_orders_entry";
        const string commerceAccess = "commerce.online_order.orders.access";

        Assert.True(CashierPosPermissionAssignmentRules.TryGetParentCode(homeOnlineOrders, out var parent));
        Assert.Equal(commerceAccess, parent);

        var orphan = CashierPosEffectivePermissionResolver.Resolve([homeOnlineOrders]);
        Assert.DoesNotContain(homeOnlineOrders, orphan);

        var withParent = CashierPosEffectivePermissionResolver.Resolve(
        [
            commerceAccess,
            homeOnlineOrders,
        ]);
        Assert.Contains(homeOnlineOrders, withParent);
        Assert.Contains(commerceAccess, withParent);
    }
}
