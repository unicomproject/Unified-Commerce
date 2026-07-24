using E_POS.Domain.Modules.Tenant.Payment.Entities;
using Xunit;

namespace E_POS.UnitTests.Payment;

public sealed class SafePaymentDisplayTests
{
    [Theory]
    [InlineData("4242", "4242")]
    [InlineData(" 4242 ", "4242")]
    [InlineData("424", null)]
    [InlineData("42424", null)]
    [InlineData("4242a", null)]
    [InlineData("tok_live_4242", null)]
    [InlineData("4111111111114242", null)]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeLast4_AcceptsExactlyFourDigitsOnly(string? input, string? expected)
    {
        Assert.Equal(expected, SafePaymentDisplay.NormalizeLast4(input));
    }

    [Theory]
    [InlineData("VISA", "Visa")]
    [InlineData("mastercard", "Mastercard")]
    [InlineData("AMEX", "Amex")]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeBrand_NormalizesKnownBrands(string? input, string? expected)
    {
        Assert.Equal(expected, SafePaymentDisplay.NormalizeBrand(input));
    }

    [Fact]
    public void ToSanitizedCardMetadataJson_OmitsSensitiveFields()
    {
        var json = SafePaymentDisplay.ToSanitizedCardMetadataJson("VISA", "4242");
        Assert.NotNull(json);
        Assert.Contains("cardBrand", json);
        Assert.Contains("cardLast4", json);
        Assert.Contains("Visa", json);
        Assert.Contains("4242", json);
        Assert.DoesNotContain("pan", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cvv", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("4111111111114242", json);
    }

    [Fact]
    public void FormatMaskedReference_UsesProjectConvention()
    {
        Assert.Equal("•••• 4242", SafePaymentDisplay.FormatMaskedReference("4242"));
        Assert.Null(SafePaymentDisplay.FormatMaskedReference("tok_live_xxx"));
        Assert.Null(SafePaymentDisplay.FormatMaskedReference(null));
    }

    [Fact]
    public void ResolveLast4_NeverTruncatesProviderTokens()
    {
        Assert.Null(SafePaymentDisplay.ResolveLast4(null, "tok_live_4242424242424242"));
        Assert.Equal("4242", SafePaymentDisplay.ResolveLast4(
            """{"cardBrand":"Visa","cardLast4":"4242"}""",
            "txn_provider_secret_abc"));
        Assert.Equal("1288", SafePaymentDisplay.ResolveLast4(null, "1288"));
    }

    [Fact]
    public void ResolveMethodLabel_PrefersBrandForCard()
    {
        Assert.Equal("Visa", SafePaymentDisplay.ResolveMethodLabel("Card", "CARD", "VISA"));
        Assert.Equal("Cash", SafePaymentDisplay.ResolveMethodLabel("Cash", "CASH", null));
    }
}
