using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantLifecycleDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("draft")]
    [InlineData("pending_payment")]
    [InlineData("pending_activation")]
    [InlineData("active")]
    [InlineData("suspended")]
    [InlineData("cancelled")]
    public void TenantCreate_AcceptsApprovedLifecycleValues(string status)
    {
        var tenant = Tenant.Create(
            Guid.NewGuid(),
            "TEN-OK",
            "ten-ok",
            "Ok",
            status,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now);

        Assert.Equal(TenantStatusConstants.Normalize(status), tenant.Status);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("paid")]
    [InlineData("setup_pending")]
    [InlineData("inactive")]
    [InlineData("monthly")]
    [InlineData("trial")]
    public void TenantCreate_RejectsNonLifecycleValues(string status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Tenant.Create(
                Guid.NewGuid(),
                "TEN-BAD",
                "ten-bad",
                "Bad",
                status,
                "LKR",
                "Asia/Colombo",
                null,
                null,
                Now));
    }

    [Fact]
    public void MarkPendingActivation_FromPendingPayment_Succeeds()
    {
        var tenant = Tenant.Create(
            Guid.NewGuid(),
            "TEN-MPA",
            "ten-mpa",
            "MPA",
            TenantStatusConstants.PendingPayment,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now);

        tenant.MarkPendingActivation(null, Now);

        Assert.Equal(TenantStatusConstants.PendingActivation, tenant.Status);
    }

    [Fact]
    public void CanActivate_ExcludesPendingPaymentActiveAndCancelled()
    {
        Assert.False(TenantLifecycleRules.CanActivate(TenantStatusConstants.PendingPayment));
        Assert.False(TenantLifecycleRules.CanActivate(TenantStatusConstants.Active));
        Assert.False(TenantLifecycleRules.CanActivate(TenantStatusConstants.Cancelled));
        Assert.True(TenantLifecycleRules.CanActivate(TenantStatusConstants.PendingActivation));
        Assert.True(TenantLifecycleRules.CanActivate(TenantStatusConstants.Draft));
    }

    [Theory]
    [InlineData("pending", null, false, "pending_payment")]
    [InlineData("paid", null, false, "pending_activation")]
    [InlineData("setup_pending", null, false, "active")]
    [InlineData("inactive", null, false, "draft")]
    [InlineData("inactive", "2026-01-01T00:00:00Z", false, "suspended")]
    [InlineData("cancelled", null, false, "cancelled")]
    [InlineData("suspended", null, false, "suspended")]
    [InlineData("active", null, false, "active")]
    public void LegacyMapper_MapsApprovedCases(
        string raw,
        string? activatedAtIso,
        bool _,
        string expected)
    {
        DateTimeOffset? activatedAt = activatedAtIso is null
            ? null
            : DateTimeOffset.Parse(activatedAtIso);

        var result = TenantLifecycleLegacyMapper.Map(raw, activatedAt);

        Assert.False(result.IsUnknown);
        Assert.Equal(expected, result.LifecycleStatus);
    }

    [Fact]
    public void LegacyMapper_UnknownValue_IsFailSafe()
    {
        var result = TenantLifecycleLegacyMapper.Map("mystery_status", activatedAt: null);

        Assert.True(result.IsUnknown);
    }

    [Theory]
    [InlineData(null, null, TenantCreateMode.Trial)]
    [InlineData("trial", "monthly", TenantCreateMode.Trial)]
    [InlineData("active", "monthly", TenantCreateMode.Paid)]
    [InlineData("trial", "demo", TenantCreateMode.Demo)]
    public void CreateModeResolver_ResolvesExpectedMode(
        string? subscriptionStatus,
        string? billingCycle,
        TenantCreateMode expected)
    {
        Assert.Equal(
            expected,
            TenantCreateModeResolver.Resolve(subscriptionStatus, billingCycle));
    }
}
