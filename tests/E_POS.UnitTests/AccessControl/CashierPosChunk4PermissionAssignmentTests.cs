using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class CashierPosChunk4PermissionAssignmentTests
{
    [Theory]
    [InlineData("pos.till.session.open/close")]
    [InlineData("pos.cash_drawer.*")]
    [InlineData("pos.sales.cart.*")]
    [InlineData("pos.payments.cash")]
    [InlineData("pos.payments.cash.accept.extra")]
    public void AssignmentRules_RejectInvalidFormats(string code)
    {
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet([code]);
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Failures,
            f => f.Kind is CashierPosPermissionAssignmentRules.FailureKind.InvalidFormat
                or CashierPosPermissionAssignmentRules.FailureKind.UnknownCanonicalPermission);
    }

    [Theory]
    [InlineData("pre_auth.login.screen.view")]
    [InlineData("pre_auth.login.branding.view")]
    [InlineData("pre_auth.login.email.input")]
    [InlineData("pre_auth.login.password.input")]
    [InlineData("pre_auth.login.password_visibility.toggle")]
    [InlineData("pre_auth.login.submit.execute")]
    [InlineData("pre_auth.login.validation.message")]
    public void AssignmentRules_RejectAllPreAuthCodes(string code)
    {
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet([code]);
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Failures,
            f => f.Kind == CashierPosPermissionAssignmentRules.FailureKind.PreAuthNotAssignable);
    }

    [Fact]
    public void AssignmentRules_RejectUnknownFourTierPosCode()
    {
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
            ["pos.sales.fake.action"]);
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Failures,
            f => f.Kind == CashierPosPermissionAssignmentRules.FailureKind.UnknownCanonicalPermission);
    }

    [Fact]
    public void AssignmentRules_AcceptIndependentPaymentMethods()
    {
        Assert.True(CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
            [PaymentPermissions.AcceptCash]).IsValid);
        Assert.True(CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
            [PaymentPermissions.AcceptCard]).IsValid);
        Assert.True(CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
            [PaymentPermissions.AcceptQr]).IsValid);
        Assert.True(CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
            [PaymentPermissions.AcceptSplit]).IsValid);
    }

    [Fact]
    public void AssignmentRules_ChildWithoutParent_IsRejected_ForNewGrant()
    {
        const string child = "pos.cash_payment.tender.exact";
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet([child]);
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Failures,
            f => f.Kind == CashierPosPermissionAssignmentRules.FailureKind.ParentPermissionRequired
                && f.PermissionCode == child
                && f.RequiredParentCode == PaymentPermissions.AcceptCash);
    }

    [Fact]
    public void AssignmentRules_ParentPlusChild_IsAccepted()
    {
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
        [
            PaymentPermissions.AcceptCash,
            "pos.cash_payment.tender.exact",
            "pos.cash_payment.numpad.container",
        ]);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AssignmentRules_OrphanedChildMayRemain_AfterParentRevoke()
    {
        const string child = "pos.cash_payment.tender.exact";
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
            targetPermissionCodes: [child],
            currentlyGrantedCodes: [PaymentPermissions.AcceptCash, child],
            availableParentCodes: [child]);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AssignmentRules_SiblingIndependence_RevokingOneChildKeepsOthers()
    {
        var result = CashierPosPermissionAssignmentRules.ValidateAssignmentSet(
        [
            PaymentPermissions.AcceptCash,
            "pos.cash_payment.numpad.container",
        ],
        currentlyGrantedCodes:
        [
            PaymentPermissions.AcceptCash,
            "pos.cash_payment.numpad.container",
            "pos.cash_payment.tender.exact",
        ]);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CashierCeiling_IncludesChunk2RoleAssignableCodes()
    {
        Assert.Contains(
            "pos.cash_payment.tender.exact",
            TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
        Assert.Contains(
            PaymentPermissions.AcceptCard,
            TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
        Assert.Contains(
            "pos.sales.held_sales.cancel",
            TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
        Assert.True(
            TenantRoleSetupCatalog.CashierAllowedPermissionCodes.IsSupersetOf(
                CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes));
    }

    [Fact]
    public void SensitiveAndNavigationChildren_AreIndependentlyRepresented()
    {
        Assert.True(CashierPosPermissionAssignmentRules.TryGetRoleAssignable(
            "pos.customers.list.phone", out var phone));
        Assert.True(phone.IsSensitive);

        Assert.True(CashierPosPermissionAssignmentRules.TryGetRoleAssignable(
            "pos.cash_drawer.summary.expected_cash", out var expectedCash));
        Assert.True(expectedCash.IsSensitive);

        Assert.True(CashierPosPermissionAssignmentRules.TryGetRoleAssignable(
            "pos.notifications.messages.body", out _));
    }

    [Fact]
    public async Task ReplacePermissions_RejectsInvalidFormat_BeforeRepositoryMutation()
    {
        var repository = new Mock<ITenantAdminRoleRepository>(MockBehavior.Strict);
        var role = TenantRole.Create(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            null,
            null,
            TenantUserConstants.DefaultCashierRoleCode,
            "Cashier",
            null,
            true,
            true,
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            DateTimeOffset.UtcNow);

        repository
            .Setup(r => r.GetEditableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repository
            .Setup(r => r.GetPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantRolePermissionsResponse(
                role.Id,
                role.RoleCode,
                role.RoleName,
                "TENANT",
                false,
                [PaymentPermissions.AcceptCash],
                [Guid.NewGuid()],
                DateTimeOffset.UtcNow));

        var service = new TenantAdminRoleService(
            repository.Object,
            Mock.Of<IIdempotencyService>(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow));

        var context = new TenantRequestContext(
            role.TenantId,
            role.CreatedByTenantUserId ?? Guid.NewGuid(),
            [TenantAdminUserPermissions.RolesPermissionsUpdate]);

        var result = await service.ReplacePermissionsAsync(
            context,
            role.Id,
            new TenantRolePermissionsUpdateRequest(["pos.cash_drawer.*"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("tenant_roles.invalid_permission_format", result.Error!.Code);
        repository.Verify(
            r => r.ReplacePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplacePermissions_RejectsPreAuth_WithoutMutation()
    {
        var repository = new Mock<ITenantAdminRoleRepository>(MockBehavior.Strict);
        var role = TenantRole.Create(
            Guid.Parse("11111111-1111-4111-8111-111111111112"),
            Guid.Parse("22222222-2222-4222-8222-222222222223"),
            null,
            null,
            TenantUserConstants.DefaultCashierRoleCode,
            "Cashier",
            null,
            true,
            true,
            Guid.Parse("33333333-3333-4333-8333-333333333334"),
            DateTimeOffset.UtcNow);

        repository
            .Setup(r => r.GetEditableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repository
            .Setup(r => r.GetPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantRolePermissionsResponse(
                role.Id,
                role.RoleCode,
                role.RoleName,
                "TENANT",
                false,
                [PaymentPermissions.AcceptCash],
                [Guid.NewGuid()],
                DateTimeOffset.UtcNow));

        var service = new TenantAdminRoleService(
            repository.Object,
            Mock.Of<IIdempotencyService>(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow));

        var context = new TenantRequestContext(
            role.TenantId,
            role.CreatedByTenantUserId ?? Guid.NewGuid(),
            [TenantAdminUserPermissions.RolesPermissionsUpdate]);

        var result = await service.ReplacePermissionsAsync(
            context,
            role.Id,
            new TenantRolePermissionsUpdateRequest(["pre_auth.login.email.input"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("tenant_roles.pre_auth_permission_not_assignable", result.Error!.Code);
        repository.Verify(
            r => r.ReplacePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplacePermissions_RejectsChildWithoutParent_WithoutMutation()
    {
        var repository = new Mock<ITenantAdminRoleRepository>(MockBehavior.Strict);
        var role = TenantRole.Create(
            Guid.Parse("11111111-1111-4111-8111-111111111113"),
            Guid.Parse("22222222-2222-4222-8222-222222222224"),
            null,
            null,
            TenantUserConstants.DefaultCashierRoleCode,
            "Cashier",
            null,
            true,
            true,
            Guid.Parse("33333333-3333-4333-8333-333333333335"),
            DateTimeOffset.UtcNow);

        repository
            .Setup(r => r.GetEditableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repository
            .Setup(r => r.GetPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantRolePermissionsResponse(
                role.Id,
                role.RoleCode,
                role.RoleName,
                "TENANT",
                false,
                [PaymentPermissions.AcceptCash],
                [Guid.NewGuid()],
                DateTimeOffset.UtcNow));

        var service = new TenantAdminRoleService(
            repository.Object,
            Mock.Of<IIdempotencyService>(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow));

        var context = new TenantRequestContext(
            role.TenantId,
            role.CreatedByTenantUserId ?? Guid.NewGuid(),
            [TenantAdminUserPermissions.RolesPermissionsUpdate]);

        var result = await service.ReplacePermissionsAsync(
            context,
            role.Id,
            new TenantRolePermissionsUpdateRequest(["pos.cash_payment.tender.exact"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("tenant_roles.parent_permission_required", result.Error!.Code);
        repository.Verify(
            r => r.ReplacePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
