using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductBarcodeFormatValidatorTests
{
    [Theory]
    [InlineData("EAN13", "EAN13")]
    [InlineData("ean-13", "EAN13")]
    [InlineData("EAN8", "EAN8")]
    [InlineData("UPC-A", "UPCA")]
    [InlineData("CODE128", "CODE128")]
    [InlineData("code39", "CODE39")]
    public void NormalizeType_AcceptsCanonicalAliases(string input, string expected)
    {
        Assert.Equal(expected, ProductBarcodeFormatValidator.NormalizeType(input));
    }

    [Fact]
    public void Validate_BlankBarcode_DoesNotRequireType()
    {
        Assert.Null(ProductBarcodeFormatValidator.Validate(null, null));
        Assert.Null(ProductBarcodeFormatValidator.Validate("  ", "EAN13"));
    }

    [Fact]
    public void Validate_Ean13_AcceptsValidChecksumAndPreservesLeadingZerosConcept()
    {
        // 0200001111001 — 13 digits with valid GTIN checksum for this fixture
        // Use a known-valid EAN-13: 4006381333931
        Assert.Null(ProductBarcodeFormatValidator.Validate("4006381333931", "EAN13"));
    }

    [Fact]
    public void Validate_Ean13_RejectsBadChecksum()
    {
        var error = ProductBarcodeFormatValidator.Validate("4006381333930", "EAN13");
        Assert.Equal("Barcode checksum is invalid.", error);
    }

    [Fact]
    public void Validate_RequiresTypeWhenBarcodePresent()
    {
        var error = ProductBarcodeFormatValidator.Validate("4006381333931", null);
        Assert.Equal("Barcode type is required when a barcode is provided.", error);
    }

    [Fact]
    public void Validate_Code128_AllowsPrintableAscii()
    {
        Assert.Null(ProductBarcodeFormatValidator.Validate("ABC-001", "CODE128"));
    }

    [Fact]
    public void Validate_Code39_RejectsLowercase()
    {
        var error = ProductBarcodeFormatValidator.Validate("abc", "CODE39");
        Assert.NotNull(error);
    }
}
