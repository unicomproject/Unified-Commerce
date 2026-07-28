using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantLifecycleApiCompatibilityTests
{
    [Fact]
    public void LifecycleStatus_OnDetailResponse_MatchesStatus_AndDiffersFromBillingCompatField()
    {
        var detail = new PlatformTenantDetailResponse(
            Guid.NewGuid(),
            "TEN-1",
            "One",
            TenantStatusConstants.PendingActivation,
            "ACTIVE",
            "unified_epos",
            "LKR",
            "Asia/Colombo",
            "en-LK",
            null,
            null,
            null,
            null,
            0,
            0,
            0,
            false,
            false,
            false,
            [],
            [],
            DateTimeOffset.UtcNow,
            null,
            null,
            false,
            true,
            false,
            false,
            LifecycleStatus: TenantStatusConstants.PendingActivation);

        Assert.Equal(detail.Status, detail.LifecycleStatus);
        Assert.NotEqual(detail.BillingStatus, detail.LifecycleStatus);
    }

    [Theory]
    [InlineData("active", true)]
    [InlineData("ACTIVE", true)]
    [InlineData("pending_payment", false)]
    [InlineData("pending_activation", false)]
    [InlineData("suspended", false)]
    [InlineData("cancelled", false)]
    [InlineData("setup_pending", false)]
    [InlineData("inactive", false)]
    public void IsTenantLoginStatusAllowed_OnlyActive(string status, bool expected)
    {
        Assert.Equal(expected, TenantAuthConstants.IsTenantLoginStatusAllowed(status));
    }
}
