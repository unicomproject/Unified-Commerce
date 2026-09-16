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
    [InlineData("unknown", "UNKNOWN")]
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

    [Theory]
    [InlineData("96385074", "GTIN8")]
    [InlineData("012345678905", "GTIN12")]
    [InlineData("4006381333931", "GTIN13")]
    [InlineData("10012345678902", "GTIN14")]
    public void Classify_ValidGtins_IdentifiesStandard(string identifier, string expectedStandard)
    {
        var result = ProductBarcodeFormatValidator.Classify(identifier);

        Assert.True(result.IsValid);
        Assert.Equal(identifier, result.NormalizedIdentifier);
        Assert.Equal(expectedStandard, result.IdentifierStandard);
        Assert.True(result.CheckDigitApplicable);
        Assert.True(result.CheckDigitValid);
        Assert.Equal(ProductBarcodeFormatValidator.Unknown, result.BarcodeType);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void Classify_TrimsFramingWhitespace_AndPreservesLeadingZero()
    {
        var result = ProductBarcodeFormatValidator.Classify(" 012345678905 ");

        Assert.True(result.IsValid);
        Assert.Equal("012345678905", result.NormalizedIdentifier);
        Assert.Equal(ProductBarcodeFormatValidator.Gtin12, result.IdentifierStandard);
    }

    [Theory]
    [InlineData(null, "EMPTY")]
    [InlineData("", "EMPTY")]
    [InlineData("1234567", "LENGTH_NOT_SUPPORTED")]
    [InlineData("4006381333930", "CHECKSUM_FAILED")]
    public void Classify_InvalidDigitInputs_ReturnsCanonicalFailure(
        string? identifier,
        string expectedFailure)
    {
        var result = ProductBarcodeFormatValidator.Classify(identifier);

        Assert.False(result.IsValid);
        Assert.Equal(expectedFailure, result.FailureReason);
    }

    [Fact]
    public void Classify_NonNumericReportedGtin_ReturnsNonNumericFailure()
    {
        var result = ProductBarcodeFormatValidator.Classify("ABC123", "EAN13");

        Assert.False(result.IsValid);
        Assert.Equal(ProductBarcodeFormatValidator.NonNumericGtinFailureReason, result.FailureReason);
    }

    [Theory]
    [InlineData("ABC-001", "CODE128")]
    [InlineData("ABC-001", "CODE39")]
    public void Classify_ReportedCodeTypes_ClassifiesAsOther(string identifier, string symbology)
    {
        var result = ProductBarcodeFormatValidator.Classify(identifier, symbology);

        Assert.True(result.IsValid);
        Assert.Equal(ProductBarcodeFormatValidator.Other, result.IdentifierStandard);
        Assert.Equal(symbology, result.BarcodeType);
        Assert.False(result.CheckDigitApplicable);
    }

    [Fact]
    public void Validate_Unknown_AcceptsValidGtinAndPrintableCode128()
    {
        Assert.Null(ProductBarcodeFormatValidator.Validate("10012345678902", "UNKNOWN"));
        Assert.Null(ProductBarcodeFormatValidator.Validate("ABC-001", "UNKNOWN"));
    }

    [Fact]
    public void Validate_Unknown_RejectsInvalidGtinChecksum()
    {
        Assert.Equal(
            "Barcode checksum is invalid.",
            ProductBarcodeFormatValidator.Validate("4006381333930", "UNKNOWN"));
    }

    [Fact]
    public void CanonicalTypes_DoesNotExposeGtin14AsBarcodeType()
    {
        Assert.Contains(
            ProductBarcodeFormatValidator.CanonicalTypes,
            type => type.Code == ProductBarcodeFormatValidator.Unknown);
        Assert.DoesNotContain(
            ProductBarcodeFormatValidator.CanonicalTypes,
            type => type.Code is "GTIN14" or "ITF14" or "EAN14");
    }
}
