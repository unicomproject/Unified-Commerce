using E_POS.Application.Modules.Tenant.OnlineStoreSetup;
using Xunit;

namespace E_POS.UnitTests.OnlineStore;

public sealed class OnlineStoreContractRulesTests
{
    [Fact]
    public void ReleaseOnePolicy_MatchesApprovedCheckoutContract()
    {
        Assert.Equal("R1", OnlineStoreReleaseOnePolicy.Release);
        Assert.True(OnlineStoreReleaseOnePolicy.CustomerRegistrationRequired);
        Assert.Equal("REGISTRATION_REQUIRED", OnlineStoreReleaseOnePolicy.CustomerAccountMode);
        Assert.False(OnlineStoreReleaseOnePolicy.GuestCheckoutAvailable);
        Assert.Equal("NOT_AVAILABLE", OnlineStoreReleaseOnePolicy.GuestCheckoutMode);
        Assert.True(OnlineStoreReleaseOnePolicy.EmailVerificationRequired);
        Assert.Equal("CLICK_COLLECT", OnlineStoreReleaseOnePolicy.FulfilmentMode);
        Assert.Equal("PAY_AT_PICKUP", OnlineStoreReleaseOnePolicy.PaymentMode);
    }

    [Theory]
    [InlineData(" Arena--Store ", "arena-store")]
    [InlineData("shop", null)]
    [InlineData("-arena", null)]
    [InlineData("ab", null)]
    [InlineData("arena_store", null)]
    public void NormalizeSlug_EnforcesCanonicalRules(string value, string? expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.NormalizeSlug(value));
    }

    [Theory]
    [InlineData("Store.Arena.LK.", "store.arena.lk")]
    [InlineData("https://store.arena.lk", null)]
    [InlineData("localhost", null)]
    [InlineData("store.arena.lk/path", null)]
    public void NormalizeDomain_RejectsUrlsAndInvalidHosts(string value, string? expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.NormalizeDomain(value));
    }

    [Theory]
    [InlineData("support@arena.lk", true)]
    [InlineData("not-an-email", false)]
    [InlineData("", false)]
    public void IsValidEmail_UsesMailboxValidation(string value, bool expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.IsValidEmail(value));
    }

    [Theory]
    [InlineData("<p>Returns accepted within seven days.</p>", false)]
    [InlineData("<script>alert(1)</script>", true)]
    [InlineData("<a onclick=\"steal()\">Click</a>", true)]
    [InlineData("javascript:alert(1)", true)]
    public void ContainsUnsafeMarkup_BlocksExecutableContent(string value, bool expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.ContainsUnsafeMarkup(value));
    }

    [Fact]
    public void RequiredPolicyTypes_MatchesApprovedFourPolicyContract()
    {
        Assert.Equal(
            ["CANCELLATION", "COLLECTION", "PRIVACY", "TERMS"],
            OnlineStoreContractRules.RequiredPolicyTypes.OrderBy(value => value));
        Assert.DoesNotContain("RETURN_REFUND", OnlineStoreContractRules.RequiredPolicyTypes);
    }

    [Theory]
    [InlineData(new string[0], 0, false)]
    [InlineData(new[] { "TERMS", "PRIVACY", "CANCELLATION" }, 3, false)]
    [InlineData(new[] { "TERMS", "PRIVACY", "CANCELLATION", "COLLECTION" }, 4, true)]
    [InlineData(new[] { "TERMS", "PRIVACY", "CANCELLATION", "COLLECTION", "RETURN_REFUND" }, 4, true)]
    [InlineData(new[] { "TERMS", "TERMS", "PRIVACY", "CANCELLATION", "COLLECTION" }, 4, true)]
    public void PolicyReadiness_UsesDistinctCanonicalRequiredSet(string[] policies, int expectedCount, bool expectedReady)
    {
        Assert.Equal(expectedCount, OnlineStoreContractRules.CountPublishedRequiredPolicies(policies));
        Assert.Equal(expectedReady, OnlineStoreContractRules.AreRequiredPoliciesPublished(policies));
    }

    [Theory]
    [InlineData("Mon - Fri: 9:00 AM - 6:00 PM", true)]
    [InlineData("Mon: 09:00 - 17:00; Sat: 10:00 - 14:00", true)]
    [InlineData("Mon - Fri: 6:00 PM - 9:00 AM", false)]
    [InlineData("always", false)]
    [InlineData("", false)]
    public void IsValidSupportHours_ValidatesStructuredIntervals(string value, bool expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.IsValidSupportHours(value));
    }

    [Theory]
    [InlineData("support@arena.lk", "+94 77 123 4567", "Arena Square", "Mon - Fri: 9:00 AM - 6:00 PM", true)]
    [InlineData("", "+94 77 123 4567", "Arena Square", "Mon - Fri: 9:00 AM - 6:00 PM", false)]
    [InlineData("support@arena.lk", "123", "Arena Square", "Mon - Fri: 9:00 AM - 6:00 PM", false)]
    [InlineData("support@arena.lk", "+94 77 123 4567", "", "Mon - Fri: 9:00 AM - 6:00 PM", false)]
    [InlineData("support@arena.lk", "+94 77 123 4567", "Arena Square", "", false)]
    public void IsSupportReady_RequiresAllFourMandatoryFields(
        string email,
        string phone,
        string address,
        string hours,
        bool expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.IsSupportReady(email, phone, address, hours));
    }

    [Theory]
    [InlineData("image/png", ".png", true)]
    [InlineData("image/svg+xml", ".svg", true)]
    [InlineData("image/x-icon", ".ico", true)]
    [InlineData("image/svg+xml", ".png", false)]
    [InlineData("text/html", ".svg", false)]
    public void BrandingMediaFormats_AlignWithUiContract(string mimeType, string extension, bool expected)
    {
        Assert.Equal(expected, OnlineStoreContractRules.IsSupportedBrandingMediaFormat(mimeType, extension));
    }
}
