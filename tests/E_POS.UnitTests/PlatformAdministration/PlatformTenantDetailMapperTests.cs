using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Mappers;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantDetailMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ApplyActionFlags_WithSuperAdminPermissions_EnablesExpectedActionsForActiveTenant()
    {
        var permissions = PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);
        var detail = CreateDetail("active", null);

        var mapped = PlatformTenantDetailMapper.ApplyActionFlags(detail, permissions);

        Assert.True(mapped.CanUpdate);
        Assert.False(mapped.CanActivate);
        Assert.True(mapped.CanSuspend);
        Assert.True(mapped.CanManageEntitlements);
    }

    [Fact]
    public void ApplyActionFlags_WithPendingActivationTenant_EnablesActivateWhenPermitted()
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal)
        {
            PlatformPermissionCodes.TenantsActivate
        };

        var mapped = PlatformTenantDetailMapper.ApplyActionFlags(
            CreateDetail("pending_activation", null),
            permissions);

        Assert.True(mapped.CanActivate);
        Assert.False(mapped.CanSuspend);
    }

    [Fact]
    public void ApplyActionFlags_WithTrialSubscription_EnablesSuspendWhenPermitted()
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal)
        {
            PlatformPermissionCodes.TenantsSuspend
        };

        var mapped = PlatformTenantDetailMapper.ApplyActionFlags(
            CreateDetail("inactive", new PlatformTenantDetailSubscriptionDto(
                Guid.NewGuid(),
                "Starter",
                "TRIAL",
                null,
                null,
                null)),
            permissions);

        Assert.True(mapped.CanSuspend);
    }

    [Fact]
    public void ApplyActionFlags_WithoutManagementPermissions_DisablesAllActions()
    {
        var mapped = PlatformTenantDetailMapper.ApplyActionFlags(
            CreateDetail("active", null),
            new HashSet<string>(StringComparer.Ordinal));

        Assert.False(mapped.CanUpdate);
        Assert.False(mapped.CanActivate);
        Assert.False(mapped.CanSuspend);
        Assert.False(mapped.CanManageEntitlements);
    }

    private static PlatformTenantDetailResponse CreateDetail(
        string status,
        PlatformTenantDetailSubscriptionDto? subscription)
    {
        return new PlatformTenantDetailResponse(
            Guid.NewGuid(),
            "TEN-001",
            "Tenant One",
            status,
            "paid",
            "unified_epos",
            "LKR",
            "Asia/Colombo",
            "en-LK",
            "Retail",
            null,
            null,
            subscription,
            1,
            1,
            1,
            false,
            false,
            false,
            Now,
            Now,
            Now,
            false,
            false,
            false,
            false);
    }
}
